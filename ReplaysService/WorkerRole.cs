using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Ninject;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal class WorkerRole : WorkerRoleBase<IReplaysSettings>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WorkerRole));

        public WorkerRole(IReplaysSettings settings, TelemetryClient telemetryClient)
            : this(settings, telemetryClient, runOnce: false, kernel: null, log: null) { }

        internal WorkerRole(IReplaysSettings settings, TelemetryClient telemetryClient, bool runOnce, IKernel kernel, ILog log) :
            base("replays", settings, telemetryClient, runOnce)
        {
            kernel = kernel ?? KernelConfig.CreateKernel();
            kernel.Bind<IReplaysSettings>()
                  .ToConstant(settings);
            kernel.Bind<TelemetryClient>()
                  .ToConstant(telemetryClient);
            this.kernel = kernel;

            this.log = log ?? Log;
        }

        private readonly IKernel kernel;
        private readonly ILog log;

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update replays cycle"))
            using (new UpdateActivity(log, "replays cycle"))
            {
                try
                {
                    if (Settings.SteamWebApiKey == null)
                    {
                        log.Warn("Using test data for calls to Steam Web API. Set your Steam Web API key to use the actual Steam Web API.");
                        log.Warn("Run this application with --help to find out how to set your Steam Web API key.");
                    }

                    await UpdateReplaysAsync(cancellationToken).ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (Exception) when (operation.Telemetry.MarkAsUnsuccessful()) { }
            }
        }

        private async Task UpdateReplaysAsync(CancellationToken cancellationToken)
        {
            var worker = kernel.Get<ReplaysWorker>();
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update replays"))
            using (new UpdateActivity(log, "replays"))
            {
                try
                {
                    var replays = await worker.GetReplaysAsync(Settings.ReplaysPerUpdate, cancellationToken).ConfigureAwait(false);
                    await worker.UpdateReplaysAsync(replays, cancellationToken).ConfigureAwait(false);
                    await worker.StoreReplaysAsync(replays, cancellationToken).ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (HttpRequestStatusException ex)
                {
                    TelemetryClient.TrackException(ex);
                    log.Error("Failed to complete run due to an error.", ex);
                    operation.Telemetry.Success = false;
                }
                catch (Exception) when (operation.Telemetry.MarkAsUnsuccessful()) { }
                finally
                {
                    kernel.Release(worker);
                }
            }
        }

        #region IDisposable Implementation

        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (disposed) { return; }

            if (disposing)
            {
                try
                {
                    kernel.Dispose();
                }
                catch (Exception) { }
            }
            disposed = true;

            base.Dispose(disposing);
        }

        #endregion
    }
}
