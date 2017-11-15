using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;
using toofz.NecroDancer.Replays;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal sealed class ReplaysWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ReplaysWorker));

        private static readonly ReplayDataSerializer ReplayDataSerializer = new ReplayDataSerializer();

        public ReplaysWorker(uint appId, TelemetryClient telemetryClient)
        {
            this.appId = appId;
            this.telemetryClient = telemetryClient;
        }

        private readonly uint appId;
        private readonly TelemetryClient telemetryClient;

        public async Task<IEnumerable<Replay>> GetReplaysAsync(
            IToofzApiClient toofzApiClient,
            int limit,
            CancellationToken cancellationToken)
        {
            var response = await toofzApiClient
                .GetReplaysAsync(new GetReplaysParams
                {
                    Limit = limit,
                }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var replays = (from r in response.Replays
                           select new Replay
                           {
                               ReplayId = r.Id,
                               ErrorCode = r.Error,
                               Seed = r.Seed,
                               Version = r.Version,
                               KilledBy = r.KilledBy,
                           })
                           .ToList();

            return replays;
        }

        public async Task UpdateReplaysAsync(
            ISteamWebApiClient steamWebApiClient,
            IUgcHttpClient ugcHttpClient,
            ICloudBlobDirectory directory,
            IEnumerable<Replay> replays,
            CancellationToken cancellationToken)
        {
            // TODO: This ends up wrapping both the downloading of replays and storing of replay files into one operation. 
            //       Those processes should be separated.
            //
            // Possible solutions include:
            //   - Integrating operations into the dataflow network like how activity tracing is currently done.
            //     Would need to determine how to mark an operation's outcome while working in a dataflow network.
            //   - Separate the operations. This has performance implications (replay files cannot be stored until ALL replays 
            //     are downloaded). However, it would logically separate the operations and simplify the code.
            using (var operation = telemetryClient.StartOperation<RequestTelemetry>("Download replays"))
            {
                try
                {
                    var replayNetwork = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);

                    foreach (var replay in replays)
                    {
                        await replayNetwork.SendAsync(replay, cancellationToken).ConfigureAwait(false);
                    }

                    replayNetwork.Complete();
                    await replayNetwork.Completion.ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (Exception)
                {
                    operation.Telemetry.Success = false;
                    throw;
                }
            }
        }

        public async Task StoreReplaysAsync(
            IToofzApiClient toofzApiClient,
            IEnumerable<Replay> replays,
            CancellationToken cancellationToken)
        {
            using (var operation = telemetryClient.StartOperation<RequestTelemetry>("Store replays"))
            using (var activity = new StoreActivity(Log, "replays"))
            {
                try
                {
                    var bulkStore = await toofzApiClient.PostReplaysAsync(replays, cancellationToken).ConfigureAwait(false);
                    activity.Report(bulkStore.RowsAffected);

                    operation.Telemetry.Success = true;
                }
                catch (Exception)
                {
                    operation.Telemetry.Success = false;
                    throw;
                }
            }
        }
    }
}
