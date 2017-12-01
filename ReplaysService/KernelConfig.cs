using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net.Http;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage;
using Ninject;
using Ninject.Activation;
using Ninject.Extensions.NamedScope;
using Polly;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.Workshop;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal static class KernelConfig
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        public static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            try
            {
                RegisterServices(kernel);

                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(StandardKernel kernel)
        {
            kernel.Bind<ILog>()
                  .ToConstant(Log);

            kernel.Bind<uint>()
                  .ToMethod(GetAppId)
                  .WhenInjectedInto<ReplaysWorker>();

            kernel.Bind<string>()
                  .ToMethod(GetLeaderboardsConnectionString)
                  .WhenInjectedInto(typeof(LeaderboardsContext), typeof(LeaderboardsStoreClient));

            kernel.Bind<ILeaderboardsContext>()
                  .To<LeaderboardsContext>()
                  .When(DatabaseContainsReplays)
                  .InParentScope();
            kernel.Bind<ILeaderboardsContext>()
                  .To<FakeLeaderboardsContext>()
                  .InParentScope();

            kernel.Bind<ILeaderboardsStoreClient>()
                  .To<LeaderboardsStoreClient>()
                  .When(SteamWebApiKeyIsSet)
                  .InParentScope();
            kernel.Bind<ILeaderboardsStoreClient>()
                  .To<FakeLeaderboardsStoreClient>()
                  .InParentScope();

            kernel.Bind<HttpMessageHandler>()
                  .ToMethod(GetSteamWebApiClientHandler)
                  .WhenInjectedInto<SteamWebApiClient>()
                  .InParentScope();
            kernel.Bind<ISteamWebApiClient>()
                  .To<SteamWebApiClient>()
                  .When(SteamWebApiKeyIsSet)
                  .InParentScope()
                  .WithPropertyValue(nameof(SteamWebApiClient.SteamWebApiKey), GetSteamWebApiKey);
            kernel.Bind<ISteamWebApiClient>()
                  .To<FakeSteamWebApiClient>()
                  .InParentScope();

            kernel.Bind<HttpMessageHandler>()
                  .ToMethod(GetUgcHttpClientHandler)
                  .WhenInjectedInto<UgcHttpClient>()
                  .InParentScope();
            kernel.Bind<IUgcHttpClient>()
                  .To<UgcHttpClient>()
                  .When(SteamWebApiKeyIsSet)
                  .InParentScope();
            kernel.Bind<IUgcHttpClient>()
                  .To<FakeUgcHttpClient>()
                  .InParentScope();

            kernel.Bind<ICloudBlobContainer>()
                  .ToMethod(GetCloudBlobContainer)
                  .InParentScope();
            kernel.Bind<ICloudBlobDirectoryFactory>()
                  .To<CloudBlobDirectoryFactory>()
                  .InParentScope();

            kernel.Bind<ReplaysWorker>()
                  .ToSelf()
                  .InScope(c => c);
        }

        private static uint GetAppId(IContext c)
        {
            return c.Kernel.Get<IReplaysSettings>().AppId;
        }

        private static string GetLeaderboardsConnectionString(IContext c)
        {
            var settings = c.Kernel.Get<IReplaysSettings>();

            if (settings.LeaderboardsConnectionString == null)
            {
                var connectionFactory = new LocalDbConnectionFactory("mssqllocaldb");
                using (var connection = connectionFactory.CreateConnection("NecroDancer"))
                {
                    settings.LeaderboardsConnectionString = new EncryptedSecret(connection.ConnectionString, settings.KeyDerivationIterations);
                    settings.Save();
                }
            }

            return settings.LeaderboardsConnectionString.Decrypt();
        }

        private static bool DatabaseContainsReplays(IRequest r)
        {
            using (var db = r.ParentContext.Kernel.Get<LeaderboardsContext>())
            {
                return db.Replays.Any();
            }
        }

        private static bool SteamWebApiKeyIsSet(IRequest r)
        {
            return r.ParentContext.Kernel.Get<IReplaysSettings>().SteamWebApiKey != null;
        }

        private static string GetSteamWebApiKey(IContext c)
        {
            return c.Kernel.Get<IReplaysSettings>().SteamWebApiKey.Decrypt();
        }

        #region SteamWebApiClient

        private static HttpMessageHandler GetSteamWebApiClientHandler(IContext c)
        {
            var log = c.Kernel.Get<ILog>();
            var telemetryClient = c.Kernel.Get<TelemetryClient>();

            return CreateSteamWebApiClientHandler(new WebRequestHandler(), log, telemetryClient);
        }

        internal static HttpMessageHandler CreateSteamWebApiClientHandler(HttpMessageHandler innerHandler, ILog log, TelemetryClient telemetryClient)
        {
            var policy = SteamWebApiClient
                .GetRetryStrategy()
                .WaitAndRetryAsync(
                    3,
                    ExponentialBackoff.GetSleepDurationProvider(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(2)),
                    (ex, duration) =>
                    {
                        telemetryClient.TrackException(ex);
                        if (log.IsDebugEnabled) { log.Debug($"Retrying in {duration}...", ex); }
                    });

            return HttpClientFactory.CreatePipeline(innerHandler, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new GZipHandler(),
                new TransientFaultHandler(policy),
            });
        }

        #endregion

        #region UgcHttpClient

        private static HttpMessageHandler GetUgcHttpClientHandler(IContext c)
        {
            return CreateUgcHttpClientHandler(new WebRequestHandler());
        }

        internal static HttpMessageHandler CreateUgcHttpClientHandler(HttpMessageHandler innerHandler)
        {
            var policy = Policy.NoOpAsync();

            return HttpClientFactory.CreatePipeline(innerHandler, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new GZipHandler(),
                new TransientFaultHandler(policy),
            });
        }

        #endregion

        private static ICloudBlobContainer GetCloudBlobContainer(IContext c)
        {
            var settings = c.Kernel.Get<IReplaysSettings>();

            if (settings.AzureStorageConnectionString == null)
            {
                var azureStorageConnectionString = CloudStorageAccount.DevelopmentStorageAccount.ToString(exportSecrets: true);
                settings.AzureStorageConnectionString = new EncryptedSecret(azureStorageConnectionString, settings.KeyDerivationIterations);
                settings.Save();
            }

            var account = CloudStorageAccount.Parse(settings.AzureStorageConnectionString.Decrypt());
            var blobClient = new CloudBlobClientAdapter(account.CreateCloudBlobClient());

            return blobClient.GetContainerReference("crypt");
        }
    }
}
