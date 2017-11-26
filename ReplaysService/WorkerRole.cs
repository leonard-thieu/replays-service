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

        public WorkerRole(IReplaysSettings settings, TelemetryClient telemetryClient) : base("replays", settings, telemetryClient)
        {
            kernel = KernelConfig.CreateKernel(settings, telemetryClient);
        }

        private readonly IKernel kernel;

        protected override async Task RunAsyncOverride(CancellationToken cancellationToken)
        {
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update replays cycle"))
            using (new UpdateActivity(Log, "replays cycle"))
            {
                try
                {
                    await UpdateReplaysAsync(cancellationToken).ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (Exception) when (Util.FailTelemetry(operation.Telemetry))
                {
                    // Unreachable
                    throw;
                }
            }
        }

        private async Task UpdateReplaysAsync(CancellationToken cancellationToken)
        {
            var worker = kernel.Get<ReplaysWorker>();
            using (var operation = TelemetryClient.StartOperation<RequestTelemetry>("Update replays"))
            using (new UpdateActivity(Log, "replays"))
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
                    Log.Error("Failed to complete run due to an error.", ex);
                    operation.Telemetry.Success = false;
                }
                catch (Exception) when (Util.FailTelemetry(operation.Telemetry))
                {
                    // Unreachable
                    throw;
                }
                finally
                {
                    kernel.Release(worker);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                kernel.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
