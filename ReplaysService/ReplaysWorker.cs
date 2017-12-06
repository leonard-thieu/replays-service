using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using toofz.Data;
using toofz.NecroDancer.Replays;
using toofz.Steam.WebApi;
using toofz.Steam.Workshop;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal sealed class ReplaysWorker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ReplaysWorker));

        private static readonly ReplayDataSerializer ReplayDataSerializer = new ReplayDataSerializer();

        public ReplaysWorker(
            uint appId,
            ILeaderboardsContext db,
            ISteamWebApiClient steamWebApiClient,
            IUgcHttpClient ugcHttpClient,
            ICloudBlobDirectoryFactory directoryFactory,
            ILeaderboardsStoreClient storeClient,
            TelemetryClient telemetryClient)
        {
            this.appId = appId;
            this.db = db;
            this.steamWebApiClient = steamWebApiClient;
            this.ugcHttpClient = ugcHttpClient;
            this.directoryFactory = directoryFactory;
            this.storeClient = storeClient;
            this.telemetryClient = telemetryClient;
        }

        private readonly uint appId;
        private readonly ILeaderboardsContext db;
        private readonly ISteamWebApiClient steamWebApiClient;
        private readonly IUgcHttpClient ugcHttpClient;
        private readonly ICloudBlobDirectoryFactory directoryFactory;
        private readonly ILeaderboardsStoreClient storeClient;
        private readonly TelemetryClient telemetryClient;

        public Task<List<Replay>> GetReplaysAsync(
            int limit,
            CancellationToken cancellationToken)
        {
            return (from r in db.Replays.AsNoTracking()
                    where r.Version == null
                    where r.ErrorCode == null
                    orderby r.ReplayId
                    select r)
                    .Take(() => limit)
                    .ToListAsync(cancellationToken);
        }

        public async Task UpdateReplaysAsync(
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
                    var directory = await directoryFactory.GetCloudBlobDirectoryAsync("replays", cancellationToken).ConfigureAwait(false);
                    var replayNetwork = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);

                    foreach (var replay in replays)
                    {
                        await replayNetwork.SendAsync(replay, cancellationToken).ConfigureAwait(false);
                    }

                    replayNetwork.Complete();
                    await replayNetwork.Completion.ConfigureAwait(false);

                    operation.Telemetry.Success = true;
                }
                catch (Exception) when (operation.Telemetry.MarkAsUnsuccessful()) { }
            }
        }

        public async Task StoreReplaysAsync(
            IEnumerable<Replay> replays,
            CancellationToken cancellationToken)
        {
            using (var operation = telemetryClient.StartOperation<RequestTelemetry>("Store replays"))
            using (var activity = new StoreActivity(Log, "replays"))
            {
                try
                {
                    var rowsAffected = await storeClient.BulkUpsertAsync(replays, cancellationToken).ConfigureAwait(false);
                    activity.Report(rowsAffected);

                    operation.Telemetry.Success = true;
                }
                catch (Exception) when (operation.Telemetry.MarkAsUnsuccessful()) { }
            }
        }
    }
}
