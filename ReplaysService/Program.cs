using System;
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

        static int Main(string[] args)
        {
            Log.Debug("Initialized logging.");

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var settings = Settings.Default;

            // Args are only allowed while running as a console as they may require user input.
            if (Environment.UserInteractive && args.Any())
            {
                var parser = new ReplaysArgsParser(Console.In, Console.Out, Console.Error, settings.KeyDerivationIterations);

                return parser.Parse(args, settings);
            }

            var instrumentationKey = settings.InstrumentationKey;
            if (!string.IsNullOrEmpty(instrumentationKey)) { TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey; }
            else
            {
                Log.Warn($"The setting 'ReplaysInstrumentationKey' is not set. Telemetry is disabled.");
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }

            Application.Run<WorkerRole, IReplaysSettings>();

            return 0;
        }
    }
}
