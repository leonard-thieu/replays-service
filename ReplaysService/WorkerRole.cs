using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;
using toofz.NecroDancer.Replays;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    sealed class WorkerRole : WorkerRoleBase<IReplaysSettings>
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        internal static CloudBlobDirectory GetDirectory(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("crypt");
            container.CreateIfNotExists();
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            return container.GetDirectoryReference("replays");
        }

        public WorkerRole(IReplaysSettings settings) : base("replays", settings) { }

        TelemetryClient telemetryClient;
        OAuth2Handler toofzOAuth2Handler;
        HttpMessageHandler toofzApiHandlers;

        protected override void OnStart(string[] args)
        {
            telemetryClient = new TelemetryClient();
            toofzOAuth2Handler = new OAuth2Handler();
            toofzApiHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            }, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new HttpRequestStatusHandler(),
                toofzOAuth2Handler,
            });

            base.OnStart(args);
        }

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Settings.ToofzApiBaseAddress))
            {
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiBaseAddress)} is not set.");
            }
            var toofzApiBaseAddress = Settings.ToofzApiBaseAddress;

            if (Settings.SteamWebApiKey == null)
            {
                throw new InvalidOperationException($"{nameof(Settings.SteamWebApiKey)} is not set.");
            }
            var steamWebApiKey = Settings.SteamWebApiKey.Decrypt();

            if (string.IsNullOrEmpty(Settings.ToofzApiUserName))
            {
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiUserName)} is not set.");
            }
            toofzOAuth2Handler.UserName = Settings.ToofzApiUserName;
            if (Settings.ToofzApiPassword == null)
            {
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiPassword)} is not set.");
            }
            toofzOAuth2Handler.Password = Settings.ToofzApiPassword.Decrypt();

            if (Settings.AzureStorageConnectionString == null)
            {
                throw new InvalidOperationException($"{nameof(Settings.AzureStorageConnectionString)} is not set.");
            }
            var azureStorageConnectionString = Settings.AzureStorageConnectionString.Decrypt();

            var steamApiHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler(), new DelegatingHandler[]
            {
                new LoggingHandler(),
                new SteamWebApiTransientFaultHandler(telemetryClient),
            });

            var ugcHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler(), new DelegatingHandler[]
            {
                new LoggingHandler(),
                new HttpRequestStatusHandler(),
            });

            using (var toofzApiClient = new ToofzApiClient(toofzApiHandlers) { BaseAddress = new Uri(toofzApiBaseAddress) })
            using (var steamWebApiClient = new SteamWebApiClient(steamApiHandlers) { SteamWebApiKey = steamWebApiKey })
            using (var ugcHttpClient = new UgcHttpClient(ugcHandlers))
            {
                await UpdateReplaysAsync(
                    toofzApiClient,
                    steamWebApiClient,
                    ugcHttpClient,
                    GetDirectory(azureStorageConnectionString),
                    Settings.ReplaysPerUpdate,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        static readonly ReplaySerializer ReplaySerializer = new ReplaySerializer();

        internal async Task UpdateReplaysAsync(
            IToofzApiClient toofzApiClient,
            ISteamWebApiClient steamWebApiClient,
            IUgcHttpClient ugcHttpClient,
            CloudBlobDirectory directory,
            int limit,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (toofzApiClient == null)
                throw new ArgumentNullException(nameof(toofzApiClient));
            if (steamWebApiClient == null)
                throw new ArgumentNullException(nameof(steamWebApiClient));
            if (ugcHttpClient == null)
                throw new ArgumentNullException(nameof(ugcHttpClient));
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            using (new UpdateNotifier(Log, "replays"))
            {
                var response = await toofzApiClient
                    .GetReplaysAsync(new GetReplaysParams
                    {
                        Limit = limit,
                    }, cancellationToken)
                    .ConfigureAwait(false);
                var ugcIds = (from r in response.replays
                              select r.id)
                             .ToList();

                var replays = new ConcurrentBag<Replay>();
                using (var download = new DownloadNotifier(Log, "replays"))
                {
                    var requests = new List<Task>();
                    foreach (var ugcId in ugcIds)
                    {
                        var request = UpdateReplayAsync(ugcId);
                        requests.Add(request);
                    }
                    await Task.WhenAll(requests).ConfigureAwait(false);

                    async Task UpdateReplayAsync(long ugcId)
                    {
                        var replay = new Replay { ReplayId = ugcId };
                        replays.Add(replay);

                        try
                        {
                            var ugcFileDetails = await steamWebApiClient.GetUgcFileDetailsAsync(Settings.AppId, ugcId, download.Progress, cancellationToken).ConfigureAwait(false);
                            try
                            {
                                var ugcFile = await ugcHttpClient.GetUgcFileAsync(ugcFileDetails.Data.Url, download.Progress, cancellationToken).ConfigureAwait(false);
                                try
                                {
                                    var replayData = ReplaySerializer.Deserialize(ugcFile);
                                    replay.Version = replayData.Header.Version;
                                    replay.KilledBy = replayData.Header.KilledBy;
                                    if (replayData.TryGetSeed(out int seed))
                                    {
                                        replay.Seed = seed;
                                    }

                                    ugcFile.Dispose();
                                    ugcFile = new MemoryStream();
                                    ReplaySerializer.Serialize(ugcFile, replayData);
                                    ugcFile.Position = 0;
                                }
                                // TODO: Catch a more specific exception.
                                catch (Exception ex)
                                {
                                    Log.Error($"Unable to read replay from '{ugcFileDetails.Data.Url}'.", ex);
                                    // Upload unmodified data on failure
                                    ugcFile.Position = 0;
                                }
                                finally
                                {
                                    try
                                    {
                                        var blob = directory.GetBlockBlobReference(replay.FileName);
                                        blob.Properties.ContentType = "application/octet-stream";
                                        blob.Properties.CacheControl = "max-age=604800"; // 1 week

                                        await blob.UploadFromStreamAsync(ugcFile, cancellationToken).ConfigureAwait(false);

                                        Log.Debug(blob.Uri);
                                    }
                                    // TODO: Catch a more specific exception.
                                    catch (Exception ex)
                                    {
                                        Log.Error($"Failed to upload {replay.FileName}.", ex);
                                    }
                                }
                            }
                            catch (HttpRequestStatusException ex)
                            {
                                replay.ErrorCode = -(int)ex.StatusCode;
                            }
                        }
                        catch (HttpRequestStatusException ex)
                        {
                            replay.ErrorCode = (int)ex.StatusCode;
                        }
                    }
                }

                using (var activity = new StoreNotifier(Log, "replays"))
                {
                    // TODO: Add rollback to stored replays in case this fails
                    var bulkStore = await toofzApiClient.PostReplaysAsync(replays, cancellationToken).ConfigureAwait(false);
                    activity.Progress.Report(bulkStore.rows_affected);
                }
            }
        }
    }
}
