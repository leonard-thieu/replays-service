using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using log4net;
using Microsoft.ApplicationInsights.Extensibility;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    static class Program
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// The main entry point of the application.
        /// </summary>
        /// <param name="args">Arguments passed in.</param>
        /// <returns>
        /// 0 - The application ran successfully.
        /// 1 - There was an error parsing <paramref name="args"/>.
        /// </returns>
        [ExcludeFromCodeCoverage]
        static int Main(string[] args)
        {
            var settings = Settings.Default;

            return MainImpl(
                args,
                Log,
                new EnvironmentAdapter(),
                new ReplaysArgsParser(Console.In, Console.Out, Console.Error, settings.KeyDerivationIterations),
                settings,
                new Application());
        }

        internal static int MainImpl(
            string[] args,
            ILog log,
            IEnvironment environment,
            IArgsParser<IReplaysSettings> parser,
            IReplaysSettings settings,
            IApplication application)
        {
            log.Debug("Initialized logging.");

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // Args are only allowed while running as a console as they may require user input.
            if (args.Any() && environment.UserInteractive)
            {
                return parser.Parse(args, settings);
            }

            if (string.IsNullOrEmpty(settings.InstrumentationKey))
            {
                log.Warn("The setting 'InstrumentationKey' is not set. Telemetry is disabled.");
            }
            else
            {
                TelemetryConfiguration.Active.InstrumentationKey = settings.InstrumentationKey;
            }

            application.Run<WorkerRole, IReplaysSettings>();

            return 0;
        }
    }
}
