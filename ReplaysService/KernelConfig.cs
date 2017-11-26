using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net.Http;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage;
using Ninject;
using Ninject.Extensions.NamedScope;
using Ninject.Syntax;
using Polly;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
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
        public static IKernel CreateKernel(IReplaysSettings settings, TelemetryClient telemetryClient)
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<IReplaysSettings>()
                      .ToConstant(settings);
                kernel.Bind<TelemetryClient>()
                      .ToConstant(telemetryClient);

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
                  .ToMethod(c => c.Kernel.Get<IReplaysSettings>().AppId)
                  .WhenInjectedInto(typeof(ReplaysWorker));

            kernel.Bind<string>()
                  .ToMethod(c =>
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
                  })
                  .WhenInjectedInto(typeof(LeaderboardsContext), typeof(LeaderboardsStoreClient));

            kernel.Bind<ILeaderboardsContext>()
                  .To<LeaderboardsContext>()
                  .When(r =>
                  {
                      using (var db = r.ParentContext.Kernel.Get<LeaderboardsContext>())
                      {
                          return db.Replays.Any();
                      }
                  })
                  .InParentScope();
            kernel.Bind<ILeaderboardsContext>()
                  .To<FakeLeaderboardsContext>()
                  .InParentScope();

            kernel.Bind<ILeaderboardsStoreClient>()
                  .To<LeaderboardsStoreClient>()
                  .WhenSteamWebApiKeyIsSet()
                  .InParentScope();
            kernel.Bind<ILeaderboardsStoreClient>()
                  .To<FakeLeaderboardsStoreClient>()
                  .InParentScope();

            RegisterSteamWebApiClient(kernel);
            RegisterUgcHttpClient(kernel);

            kernel.Bind<ICloudBlobContainer>()
                  .ToMethod(c =>
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
                  })
                  .InParentScope();
            kernel.Bind<ICloudBlobDirectoryFactory>()
                  .To<CloudBlobDirectoryFactory>()
                  .InParentScope();

            kernel.Bind<ReplaysWorker>()
                  .ToSelf()
                  .InScope(c => c);
        }

        #region SteamWebApiClient

        private static void RegisterSteamWebApiClient(StandardKernel kernel)
        {
            kernel.Bind<HttpMessageHandler>()
                  .ToMethod(c =>
                  {
                      var log = c.Kernel.Get<ILog>();
                      var telemetryClient = c.Kernel.Get<TelemetryClient>();

                      return CreateSteamWebApiClientHandler(new WebRequestHandler(), log, telemetryClient);
                  })
                  .WhenInjectedInto(typeof(SteamWebApiClient))
                  .InParentScope();
            kernel.Bind<ISteamWebApiClient>()
                  .To<SteamWebApiClient>()
                  .WhenSteamWebApiKeyIsSet()
                  .InParentScope()
                  .WithPropertyValue(
                      nameof(SteamWebApiClient.SteamWebApiKey),
                      c => c.Kernel.Get<IReplaysSettings>().SteamWebApiKey.Decrypt());

            kernel.Bind<ISteamWebApiClient>()
                  .To<FakeSteamWebApiClient>()
                  .InParentScope();
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

        private static void RegisterUgcHttpClient(StandardKernel kernel)
        {
            kernel.Bind<HttpMessageHandler>()
                  .ToMethod(c =>
                  {
                      return CreateUgcHttpClientHandler(new WebRequestHandler());
                  })
                  .WhenInjectedInto(typeof(UgcHttpClient))
                  .InParentScope();
            kernel.Bind<IUgcHttpClient>()
                  .To<UgcHttpClient>()
                  .WhenSteamWebApiKeyIsSet()
                  .InParentScope();

            kernel.Bind<IUgcHttpClient>()
                  .To<FakeUgcHttpClient>()
                  .InParentScope();
        }

        internal static HttpMessageHandler CreateUgcHttpClientHandler(HttpMessageHandler innerHandler)
        {
            return HttpClientFactory.CreatePipeline(innerHandler, new DelegatingHandler[]
            {
                new LoggingHandler(),
                new GZipHandler(),
                new HttpErrorHandler(),
            });
        }

        #endregion
    }

    internal static class IBindingWhenSyntaxExtensions
    {
        public static IBindingInNamedWithOrOnSyntax<T> WhenSteamWebApiKeyIsSet<T>(this IBindingWhenSyntax<T> binding)
        {
            return binding.When(r => r.ParentContext.Kernel.Get<IReplaysSettings>().SteamWebApiKey != null);
        }
    }
}
