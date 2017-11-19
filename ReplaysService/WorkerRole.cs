using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Polly;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal class WorkerRole : WorkerRoleBase<IReplaysSettings>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        internal static async Task<ICloudBlobDirectory> GetCloudBlobDirectory(
            ICloudBlobClient blobClient,
            CancellationToken cancellationToken)
        {
            var container = blobClient.GetContainerReference("crypt");
            var containerExists = await container.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!containerExists)
            {
                await container.CreateAsync(cancellationToken).ConfigureAwait(false);
            }
            var permissions = new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob };
            await container.SetPermissionsAsync(permissions, cancellationToken).ConfigureAwait(false);
            var directory = container.GetDirectoryReference("replays");

            return directory;
        }

        public WorkerRole(IReplaysSettings settings, TelemetryClient telemetryClient) : base("replays", settings, telemetryClient) { }

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update replays cycle"))
            using (new UpdateActivity(Log, "replays"))
            {
                try
                {
                    if (Settings.LeaderboardsConnectionString == null)
                        throw new InvalidOperationException($"{nameof(Settings.LeaderboardsConnectionString)} is not set.");
                    if (Settings.SteamWebApiKey == null)
                        throw new InvalidOperationException($"{nameof(Settings.SteamWebApiKey)} is not set.");
                    if (Settings.AzureStorageConnectionString == null)
                        throw new InvalidOperationException($"{nameof(Settings.AzureStorageConnectionString)} is not set.");
                    if (Settings.ReplaysPerUpdate <= 0)
                        throw new InvalidOperationException($"{nameof(Settings.ReplaysPerUpdate)} is not set to a positive number.");

                    var leaderboardsConnectionString = Settings.LeaderboardsConnectionString.Decrypt();
                    var replaysPerUpdate = Settings.ReplaysPerUpdate;
                    var azureStorageConnectionString = Settings.AzureStorageConnectionString.Decrypt();
                    var account = CloudStorageAccount.Parse(azureStorageConnectionString);
                    var steamWebApiKey = Settings.SteamWebApiKey.Decrypt();

                    var worker = new ReplaysWorker(Settings.AppId, TelemetryClient);

                    IEnumerable<Replay> replays;
                    using (var db = new LeaderboardsContext(leaderboardsConnectionString))
                    {
                        replays = await worker.GetReplaysAsync(db, replaysPerUpdate, cancellationToken).ConfigureAwait(false);
                    }

                    var blobClient = new CloudBlobClientAdapter(account.CreateCloudBlobClient());
                    var directory = await GetCloudBlobDirectory(blobClient, cancellationToken).ConfigureAwait(false);

                    using (var steamWebApiClient = CreateSteamWebApiClient(steamWebApiKey))
                    using (var ugcHttpClient = CreateUgcHttpClient())
                    {
                        await worker.UpdateReplaysAsync(steamWebApiClient, ugcHttpClient, directory, replays, cancellationToken).ConfigureAwait(false);
                    }

                    using (var connection = new SqlConnection(leaderboardsConnectionString))
                    {
                        var storeClient = new LeaderboardsStoreClient(connection);
                        await worker.StoreReplaysAsync(storeClient, replays, cancellationToken).ConfigureAwait(false);
                    }

                    operation.Telemetry.Success = true;
                }
                catch (Exception)
                {
                    operation.Telemetry.Success = false;
                    throw;
                }
            }
        }

        internal ISteamWebApiClient CreateSteamWebApiClient(string apiKey, HttpMessageHandler innerHandler = null)
        {
            innerHandler = innerHandler ?? new WebRequestHandler();

            var policy = SteamWebApiClient
                .GetRetryStrategy()
                .WaitAndRetryAsync(
                    3,
                    ExponentialBackoff.GetSleepDurationProvider(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(2)),
                    (ex, duration) =>
                    {
                        TelemetryClient.TrackException(ex);
                        if (Log.IsDebugEnabled) { Log.Debug($"Retrying in {duration}...", ex); }
                    });

            var handler = HttpClientFactory.CreatePipeline(innerHandler, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new GZipHandler(),
                new TransientFaultHandler(policy),
            });

            return new SteamWebApiClient(handler, TelemetryClient) { SteamWebApiKey = apiKey };
        }

        internal IUgcHttpClient CreateUgcHttpClient(HttpMessageHandler innerHandler = null)
        {
            innerHandler = innerHandler ?? new WebRequestHandler();

            var handler = HttpClientFactory.CreatePipeline(innerHandler, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new GZipHandler(),
                new HttpErrorHandler(),
            });

            return new UgcHttpClient(handler, TelemetryClient);
        }
    }
}
