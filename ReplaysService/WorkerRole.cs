using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using log4net;
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
    class WorkerRole : WorkerRoleBase<IReplaysSettings>
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        public WorkerRole(IReplaysSettings settings) : base("replays", settings) { }

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
            if (Settings.ReplaysPerUpdate <= 0)
                throw new InvalidOperationException($"{nameof(Settings.ReplaysPerUpdate)} is not set to a positive number.");

            var toofzApiBaseAddress = Settings.ToofzApiBaseAddress;
            var steamWebApiKey = Settings.SteamWebApiKey.Decrypt();
            var azureStorageConnectionString = Settings.AzureStorageConnectionString.Decrypt();
            var replaysPerUpdate = Settings.ReplaysPerUpdate;

            var steamApiHandlers = HttpClientFactory.CreatePipeline(new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip,
            }, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new SteamWebApiTransientFaultHandler(),
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

            using (var toofzApiClient = new ToofzApiClient(toofzApiHandlers, disposeHandler: false))
            using (var steamWebApiClient = new SteamWebApiClient(steamApiHandlers))
            using (var ugcHttpClient = new UgcHttpClient(ugcHandlers))
            {
                toofzApiClient.BaseAddress = new Uri(toofzApiBaseAddress);
                steamWebApiClient.SteamWebApiKey = steamWebApiKey;

                var account = CloudStorageAccount.Parse(azureStorageConnectionString);
                var blobClient = new CloudBlobClientAdapter(account.CreateCloudBlobClient());

                await UpdateReplaysAsync(
                    toofzApiClient,
                    steamWebApiClient,
                    ugcHttpClient,
                    blobClient,
                    replaysPerUpdate,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        static readonly ReplaySerializer ReplaySerializer = new ReplaySerializer();

        internal async Task UpdateReplaysAsync(
            IToofzApiClient toofzApiClient,
            ISteamWebApiClient steamWebApiClient,
            IUgcHttpClient ugcHttpClient,
            ICloudBlobClient blobClient,
            int limit,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new UpdateNotifier(Log, "replays"))
            {
                var staleReplays = await GetStaleReplaysAsync(toofzApiClient, limit, cancellationToken).ConfigureAwait(false);

                var container = blobClient.GetContainerReference("crypt");
                await container.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
                await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob }, cancellationToken);
                var directory = container.GetDirectoryReference("replays");

                var replays = await DownloadReplaysAndStoreReplayFilesAsync(
                    steamWebApiClient,
                    ugcHttpClient,
                    directory,
                    staleReplays,
                    cancellationToken)
                    .ConfigureAwait(false);

                await StoreReplaysAsync(toofzApiClient, replays, cancellationToken).ConfigureAwait(false);
            }
        }

        internal async Task<IEnumerable<Replay>> DownloadReplaysAndStoreReplayFilesAsync(
            ISteamWebApiClient steamWebApiClient,
            IUgcHttpClient ugcHttpClient,
            ICloudBlobDirectory directory,
            IEnumerable<ReplayDTO> staleReplays,
            CancellationToken cancellationToken)
        {
            var replayNetwork = new ReplayDataflowNetwork(steamWebApiClient, Settings.AppId, ugcHttpClient, directory, cancellationToken);

            var replays = new ConcurrentBag<Replay>();
            var getReplay = new ActionBlock<Replay>(replay =>
            {
                replays.Add(replay);
            }, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = Environment.ProcessorCount,
                CancellationToken = cancellationToken,
            });
            replayNetwork.DownloadReplay.LinkTo(getReplay, new DataflowLinkOptions { PropagateCompletion = true });

            var replayFileUris = new ConcurrentBag<Uri>();
            var getReplayFileUri = new ActionBlock<Uri>(replayFileUri =>
            {
                replayFileUris.Add(replayFileUri);
            }, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = Environment.ProcessorCount,
                CancellationToken = cancellationToken,
            });
            replayNetwork.StoreReplayFile.LinkTo(getReplayFileUri, new DataflowLinkOptions { PropagateCompletion = true });

            foreach (var staleReplay in staleReplays)
            {
                await replayNetwork.SendAsync(staleReplay.Id, cancellationToken).ConfigureAwait(false);
            }

            replayNetwork.Complete();
            await Task.WhenAll(getReplay.Completion, getReplayFileUri.Completion).ConfigureAwait(false);

            return replays;
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
                var bulkStore = await toofzApiClient.PostReplaysAsync(replays, cancellationToken).ConfigureAwait(false);
                storeNotifier.Report(bulkStore.RowsAffected);
            }
        }
    }
}
