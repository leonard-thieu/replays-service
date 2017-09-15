using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;
using toofz.NecroDancer.Replays;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    sealed class WorkerRole : WorkerRoleBase<IReplaysSettings>
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        internal static ICloudBlobDirectory GetDirectory(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("crypt");
            container.CreateIfNotExists();
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            var directory = container.GetDirectoryReference("replays");

            return new CloudBlobDirectoryAdapter(directory);
        }

        public WorkerRole(IReplaysSettings settings) : base("replays", settings) { }

        TelemetryClient telemetryClient;
        OAuth2Handler toofzOAuth2Handler;
        HttpMessageHandler toofzApiHandlers;

        protected override void OnStart(string[] args)
        {
            if (string.IsNullOrEmpty(Settings.ToofzApiUserName))
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiUserName)} is not set.");
            if (Settings.ToofzApiPassword == null)
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiPassword)} is not set.");

            var toofzApiUserName = Settings.ToofzApiUserName;
            var toofzApiPassword = Settings.ToofzApiPassword.Decrypt();

            telemetryClient = new TelemetryClient();
            toofzOAuth2Handler = new OAuth2Handler(toofzApiUserName, toofzApiPassword);
            toofzApiHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            }, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new ToofzHttpErrorHandler(),
                toofzOAuth2Handler,
            });

            base.OnStart(args);
        }

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Settings.ToofzApiBaseAddress))
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiBaseAddress)} is not set.");
            if (Settings.SteamWebApiKey == null)
                throw new InvalidOperationException($"{nameof(Settings.SteamWebApiKey)} is not set.");
            if (Settings.AzureStorageConnectionString == null)
                throw new InvalidOperationException($"{nameof(Settings.AzureStorageConnectionString)} is not set.");

            var toofzApiBaseAddress = Settings.ToofzApiBaseAddress;
            var steamWebApiKey = Settings.SteamWebApiKey.Decrypt();
            var azureStorageConnectionString = Settings.AzureStorageConnectionString.Decrypt();

            var steamApiHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            }, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new SteamWebApiTransientFaultHandler(telemetryClient),
                new ContentLengthHandler(),
            });

            var ugcHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            }, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new HttpErrorHandler(),
                new ContentLengthHandler(),
            });

            using (var toofzApiClient = new ToofzApiClient(toofzApiHandlers))
            using (var steamWebApiClient = new SteamWebApiClient(steamApiHandlers))
            using (var ugcHttpClient = new UgcHttpClient(ugcHandlers))
            {
                toofzApiClient.BaseAddress = new Uri(toofzApiBaseAddress);
                steamWebApiClient.SteamWebApiKey = steamWebApiKey;

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
            ICloudBlobDirectory directory,
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
                var staleReplays = await GetStaleReplaysAsync(toofzApiClient, limit, cancellationToken).ConfigureAwait(false);

                var replayNetwork = new ReplayDataflowNetwork(steamWebApiClient, Settings.AppId, ugcHttpClient, directory, cancellationToken);

                var downloadReplayTasks = new List<Task<Replay>>(staleReplays.Count());
                var storeReplayFileTasks = new List<Task<Uri>>(staleReplays.Count());
                foreach (var staleReplay in staleReplays)
                {
                    replayNetwork.Post(staleReplay.Id);

                    var downloadReplayTask = replayNetwork.DownloadReplay.ReceiveAsync(cancellationToken);
                    downloadReplayTasks.Add(downloadReplayTask);

                    var storeReplayFileTask = replayNetwork.StoreReplayFile.ReceiveAsync(cancellationToken);
                    storeReplayFileTasks.Add(storeReplayFileTask);
                }
                replayNetwork.Complete();

                var replays = await Task.WhenAll(downloadReplayTasks).ConfigureAwait(false);
                var replayFileUris = await Task.WhenAll(storeReplayFileTasks).ConfigureAwait(false);

                await StoreReplaysAsync(toofzApiClient, replays, cancellationToken).ConfigureAwait(false);
            }
        }

        internal static async Task<IEnumerable<ReplayDTO>> GetStaleReplaysAsync(
            IToofzApiClient toofzApiClient,
            int limit,
            CancellationToken cancellationToken)
        {
            var response = await toofzApiClient
                .GetReplaysAsync(new GetReplaysParams
                {
                    Limit = limit,
                }, cancellationToken)
                .ConfigureAwait(false);

            return response.Replays;
        }

        internal static async Task StoreReplaysAsync(
            IToofzApiClient toofzApiClient,
            IEnumerable<Replay> replays,
            CancellationToken cancellationToken)
        {
            using (var storeNotifier = new StoreNotifier(Log, "replays"))
            {
                // TODO: Add rollback to stored replays in case this fails
                var bulkStore = await toofzApiClient.PostReplaysAsync(replays, cancellationToken).ConfigureAwait(false);
                storeNotifier.Report(bulkStore.RowsAffected);
            }
        }
    }
}
