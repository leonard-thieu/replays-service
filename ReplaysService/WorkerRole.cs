using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    class WorkerRole : WorkerRoleBase<IReplaysSettings>
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        internal static HttpMessageHandler CreateToofzApiHandler(string toofzApiUserName, string toofzApiPassword, HttpMessageHandler innerHandler = null)
        {
            innerHandler = innerHandler ?? new WebRequestHandler { AutomaticDecompression = DecompressionMethods.GZip };

            return HttpClientFactory.CreatePipeline(innerHandler, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new ToofzHttpErrorHandler(),
                new OAuth2Handler(toofzApiUserName, toofzApiPassword),
            });
        }

        internal static HttpMessageHandler CreateSteamWebApiHandler(HttpMessageHandler innerHandler = null)
        {
            innerHandler = innerHandler ?? new WebRequestHandler();

            return HttpClientFactory.CreatePipeline(innerHandler, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new GZipHandler(),
                new SteamWebApiTransientFaultHandler(),
            });
        }

        internal static HttpMessageHandler CreateUgcHandler(HttpMessageHandler innerHandler = null)
        {
            innerHandler = innerHandler ?? new WebRequestHandler();

            return HttpClientFactory.CreatePipeline(innerHandler, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new GZipHandler(),
                new HttpErrorHandler(),
            });
        }

        internal static async Task<ICloudBlobDirectory> GetCloudBlobDirectory(
            CloudBlobClientAdapter blobClient,
            CancellationToken cancellationToken)
        {
            var container = blobClient.GetContainerReference("crypt");
            var containerExists = await container.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!containerExists)
            {
                await container.CreateAsync(cancellationToken).ConfigureAwait(false);
            }
            var permissions = new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob };
            await container.SetPermissionsAsync(permissions, cancellationToken);
            var directory = container.GetDirectoryReference("replays");

            return directory;
        }

        public WorkerRole(IReplaysSettings settings) : base("replays", settings) { }

        HttpMessageHandler toofzApiHandler;

        protected override void OnStart(string[] args)
        {
            if (string.IsNullOrEmpty(Settings.ToofzApiUserName))
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiUserName)} is not set.");
            if (Settings.ToofzApiPassword == null)
                throw new InvalidOperationException($"{nameof(Settings.ToofzApiPassword)} is not set.");

            var toofzApiUserName = Settings.ToofzApiUserName;
            var toofzApiPassword = Settings.ToofzApiPassword.Decrypt();
            toofzApiHandler = CreateToofzApiHandler(toofzApiUserName, toofzApiPassword);

            base.OnStart(args);
        }

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            using (new UpdateActivity(Log, "replays"))
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

                using (var toofzApiClient = new ToofzApiClient(toofzApiHandler, disposeHandler: false))
                {
                    toofzApiClient.BaseAddress = new Uri(toofzApiBaseAddress);

                    var account = CloudStorageAccount.Parse(azureStorageConnectionString);
                    var blobClient = new CloudBlobClientAdapter(account.CreateCloudBlobClient());

                    var replaysWorker = new ReplaysWorker(Settings.AppId);

                    var replays = await replaysWorker.GetReplaysAsync(toofzApiClient, replaysPerUpdate, cancellationToken).ConfigureAwait(false);

                    var directory = await GetCloudBlobDirectory(blobClient, cancellationToken).ConfigureAwait(false);

                    using (var steamWebApiClient = new SteamWebApiClient(CreateSteamWebApiHandler()))
                    using (var ugcHttpClient = new UgcHttpClient(CreateUgcHandler()))
                    {
                        steamWebApiClient.SteamWebApiKey = steamWebApiKey;

                        await replaysWorker.UpdateReplaysAsync(steamWebApiClient, ugcHttpClient, directory, replays, cancellationToken).ConfigureAwait(false);
                    }

                    await replaysWorker.StoreReplaysAsync(toofzApiClient, replays, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
