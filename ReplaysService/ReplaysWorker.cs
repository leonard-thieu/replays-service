using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
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

        public ReplaysWorker(uint appId)
        {
            this.appId = appId;
        }

        private readonly uint appId;

        public async Task<IEnumerable<Replay>> GetReplaysAsync(
            IToofzApiClient toofzApiClient,
            int limit,
            CancellationToken cancellationToken)
        {
            if (toofzApiClient == null)
                throw new ArgumentNullException(nameof(toofzApiClient));
            if (limit < 1)
                throw new ArgumentOutOfRangeException(nameof(limit), limit, $"'{nameof(limit)}' must be a positive number.");

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
            if (replays == null)
                throw new ArgumentNullException(nameof(replays));

            var replayNetwork = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);

            foreach (var replay in replays)
            {
                await replayNetwork.SendAsync(replay, cancellationToken).ConfigureAwait(false);
            }

            replayNetwork.Complete();
            await replayNetwork.Completion.ConfigureAwait(false);
        }

        public async Task StoreReplaysAsync(
            IToofzApiClient toofzApiClient,
            IEnumerable<Replay> replays,
            CancellationToken cancellationToken)
        {
            using (var activity = new StoreActivity(Log, "replays"))
            {
                if (toofzApiClient == null)
                    throw new ArgumentNullException(nameof(toofzApiClient));
                if (replays == null)
                    throw new ArgumentNullException(nameof(replays));

                var bulkStore = await toofzApiClient.PostReplaysAsync(replays, cancellationToken).ConfigureAwait(false);
                activity.Report(bulkStore.RowsAffected);
            }
        }
    }
}
