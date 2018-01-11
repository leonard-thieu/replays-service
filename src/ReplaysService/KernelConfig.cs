using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage;
using Ninject;
using Ninject.Activation;
using Ninject.Extensions.NamedScope;
using Polly;
using toofz.Data;
using toofz.Services.ReplaysService.Properties;
using toofz.Steam;
using toofz.Steam.WebApi;
using toofz.Steam.Workshop;

namespace toofz.Services.ReplaysService
{
    [ExcludeFromCodeCoverage]
    internal static class KernelConfig
    {
        // The dev database is intended for development and demonstration scenarios.
        private const string DevDatabaseName = "DevNecroDancer";

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
                  .WhenInjectedInto(typeof(NecroDancerContextOptionsBuilder), typeof(LeaderboardsStoreClient))
                  .InScope(c => UpdateCycleScope.Instance);
            kernel.Bind<DbContextOptionsBuilder<NecroDancerContext>>()
                  .To<NecroDancerContextOptionsBuilder>();
            kernel.Bind<DbContextOptions<NecroDancerContext>>()
                  .ToMethod(GetNecroDancerContextOptions);
            kernel.Bind<ILeaderboardsContext>()
                  .To<NecroDancerContext>()
                  .InParentScope()
                  .OnActivation(InitializeNecroDancerContext)
                  .OnActivation(EnsureDevSeedData);

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
                  .InParentScope()
                  .OnActivation(WarnUsingTestDataForSteamWebApi);

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

            kernel.Bind<ILeaderboardsStoreClient>()
                  .To<LeaderboardsStoreClient>()
                  .InParentScope();

            kernel.Bind<ReplaysWorker>()
                  .ToSelf()
                  .InScope(c => UpdateCycleScope.Instance)
                  .OnDeactivation(_ => UpdateCycleScope.Instance = new object());
        }

        private static uint GetAppId(IContext c)
        {
            return c.Kernel.Get<IReplaysSettings>().AppId;
        }

        #region Database

        private static string GetLeaderboardsConnectionString(IContext c)
        {
            var settings = c.Kernel.Get<IReplaysSettings>();

            // If SteamWebApiKey is not set, use the dev database as test data will be returned from Steam Web API.
            if (settings.SteamWebApiKey == null)
            {
                return StorageHelper.GetLocalDbConnectionString(DevDatabaseName);
            }

            // Get the connection string from settings if it's available; otherwise, use the default.
            var connectionString = settings.LeaderboardsConnectionString?.Decrypt() ??
                                   StorageHelper.GetLocalDbConnectionString("NecroDancer");

            // Check if any players are in the database. If there are none (i.e. toofz Leaderboards Service hasn't been run),
            // use the dev database instead as it will be seeded with test data.
            var options = new DbContextOptionsBuilder<NecroDancerContext>()
                .UseSqlServer(connectionString)
                .Options;

            using (var context = new NecroDancerContext(options))
            {
                InitializeNecroDancerContext(context);

                if (!context.Replays.Any())
                {
                    var log = c.Kernel.Get<ILog>();

                    log.Warn("No replays exist in target database.");
                    log.Warn("Using dev database with test data.");
                    log.Warn("Run toofz Leaderboards Service to update the database.");

                    connectionString = StorageHelper.GetLocalDbConnectionString(DevDatabaseName);
                }
            }

            return connectionString;
        }

        private static DbContextOptions<NecroDancerContext> GetNecroDancerContextOptions(IContext c)
        {
            return c.Kernel.Get<NecroDancerContextOptionsBuilder>().Options;
        }

        private static void InitializeNecroDancerContext(NecroDancerContext context)
        {
            context.Database.Migrate();
            context.EnsureSeedData();
        }

        private static void EnsureDevSeedData(NecroDancerContext context)
        {
            if (context.Database.GetDbConnection().Database == DevDatabaseName)
            {
                if (!context.Replays.Any())
                {
                    var ugcFileDetailsPath = Path.Combine("Data", "SteamWebApi", "UgcFileDetails");
                    var ugcFileDetailsFiles = Directory.GetFiles(ugcFileDetailsPath, "*.json");

                    foreach (var ugcFileDetailsFile in ugcFileDetailsFiles)
                    {
                        var ugcFileDetailsFileName = Path.GetFileNameWithoutExtension(ugcFileDetailsFile);
                        var replay = new Replay { ReplayId = long.Parse(ugcFileDetailsFileName) };
                        context.Replays.Add(replay);
                    }

                    context.SaveChanges();
                }
            }
        }

        #endregion

        #region SteamWebApiClient

        private static HttpMessageHandler GetSteamWebApiClientHandler(IContext c)
        {
            var telemetryClient = c.Kernel.Get<TelemetryClient>();
            var log = c.Kernel.Get<ILog>();

            return CreateSteamWebApiClientHandler(new WebRequestHandler(), log, telemetryClient);
        }

        internal static HttpMessageHandler CreateSteamWebApiClientHandler(WebRequestHandler innerHandler, ILog log, TelemetryClient telemetryClient)
        {
            var policy = Policy
                .Handle<Exception>(SteamWebApiClient.IsTransient)
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

        private static bool SteamWebApiKeyIsSet(IRequest r)
        {
            return r.ParentContext.Kernel.Get<IReplaysSettings>().SteamWebApiKey != null;
        }

        private static string GetSteamWebApiKey(IContext c)
        {
            return c.Kernel.Get<IReplaysSettings>().SteamWebApiKey.Decrypt();
        }

        private static void WarnUsingTestDataForSteamWebApi(IContext c, FakeSteamWebApiClient _)
        {
            var log = c.Kernel.Get<ILog>();

            log.Warn("Steam Web API key is not set.");
            log.Warn("Using test data for calls to Steam Web API.");
            log.Warn("Run this application with --help to find out how to set your Steam Web API key.");
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

        #region CloudBlobContainer

        private static ICloudBlobContainer GetCloudBlobContainer(IContext c)
        {
            var settings = c.Kernel.Get<IReplaysSettings>();

            var connectionString = settings.AzureStorageConnectionString?.Decrypt() ??
                                   CloudStorageAccount.DevelopmentStorageAccount.ToString(exportSecrets: true);

            return CreateCloudBlobContainer(connectionString, "crypt");
        }

        internal static ICloudBlobContainer CreateCloudBlobContainer(string connectionString, string containerName)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            var blobClient = new CloudBlobClientAdapter(account.CreateCloudBlobClient());

            return blobClient.GetContainerReference(containerName);
        }

        #endregion

        private sealed class UpdateCycleScope
        {
            public static object Instance { get; set; } = new object();
        }
    }

    internal sealed class NecroDancerContextOptionsBuilder : DbContextOptionsBuilder<NecroDancerContext>
    {
        public NecroDancerContextOptionsBuilder(string connectionString)
        {
            this.UseSqlServer(connectionString);
        }
    }
}
