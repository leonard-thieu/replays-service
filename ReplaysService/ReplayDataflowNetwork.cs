using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using log4net;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamRemoteStorage;
using toofz.NecroDancer.Replays;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    sealed class ReplayDataflowNetwork
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(ReplayDataflowNetwork));

        static readonly ReplaySerializer ReplaySerializer = new ReplaySerializer();

        public ReplayDataflowNetwork(
            ISteamWebApiClient steamWebApiClient,
            uint appId,
            IUgcHttpClient ugcHttpClient,
            ICloudBlobDirectory directory,
            CancellationToken cancellationToken)
        {
            this.steamWebApiClient = steamWebApiClient ?? throw new ArgumentNullException(nameof(steamWebApiClient));
            if (appId <= 0)
                throw new ArgumentOutOfRangeException(nameof(appId), $"{nameof(appId)} must be a positive integer.");
            this.appId = appId;
            this.ugcHttpClient = ugcHttpClient ?? throw new ArgumentNullException(nameof(ugcHttpClient));
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.cancellationToken = cancellationToken;

            broadcastUgcId = new BroadcastBlock<long>(ugcId => ugcId, GetDefaultDataflowBlockOptions());
            var getUgcFileDetails = new TransformBlock<long, UgcFileDetailsEnvelope>(
                ugcId => GetUgcFileDetailsAsync(ugcId),
                GetNetworkBoundExecutionDataflowBlockOptions());
            var getUgcFile = new TransformBlock<UgcFileDetailsEnvelope, byte[]>(
                ugcFileDetails => GetUgcFileAsync(ugcFileDetails),
                GetNetworkBoundExecutionDataflowBlockOptions());

            var broadcastUgcFile = new BroadcastBlock<byte[]>(rawReplayData => rawReplayData, GetDefaultDataflowBlockOptions());
            var readReplayData = new TransformBlock<byte[], ReplayData>(
                rawReplayData => ReadReplayData(rawReplayData),
                GetProcessorBoundExecutionDataflowBlockOptions());

            var broadcastReplayData = new BroadcastBlock<ReplayData>(replayData => replayData, GetDefaultDataflowBlockOptions());
            var joinCreateReplay = new JoinBlock<long, ReplayData>(GetDefaultGroupingDataflowBlockOptions());
            var createReplay = new TransformBlock<Tuple<long, ReplayData>, Replay>(
                p => CreateReplay(p.Item1, p.Item2),
                GetProcessorBoundExecutionDataflowBlockOptions());

            var broadcastReplay = new BroadcastBlock<Replay>(replay => replay, GetDefaultDataflowBlockOptions());
            bufferReplay = new BufferBlock<Replay>(GetDefaultDataflowBlockOptions());
            var joinStoreUgcFile = new JoinBlock<ReplayData, Replay>(GetDefaultGroupingDataflowBlockOptions());
            storeUgcFile = new TransformBlock<Tuple<ReplayData, Replay>, Uri>(
                p => StoreUgcFileAsync(p.Item1, p.Item2),
                GetNetworkBoundExecutionDataflowBlockOptions());

            broadcastUgcId.LinkTo(getUgcFileDetails, GetDefaultDataflowLinkOptions());
            getUgcFileDetails.LinkTo(getUgcFile, GetDefaultDataflowLinkOptions());
            getUgcFile.LinkTo(broadcastUgcFile, GetDefaultDataflowLinkOptions());

            broadcastUgcFile.LinkTo(readReplayData, GetDefaultDataflowLinkOptions());
            readReplayData.LinkTo(broadcastReplayData, GetDefaultDataflowLinkOptions());

            broadcastUgcId.LinkTo(joinCreateReplay.Target1, GetDefaultDataflowLinkOptions());
            broadcastReplayData.LinkTo(joinCreateReplay.Target2, GetDefaultDataflowLinkOptions());
            joinCreateReplay.LinkTo(createReplay, GetDefaultDataflowLinkOptions());
            createReplay.LinkTo(broadcastReplay, GetDefaultDataflowLinkOptions());

            broadcastReplay.LinkTo(bufferReplay, GetDefaultDataflowLinkOptions());

            broadcastReplayData.LinkTo(joinStoreUgcFile.Target1, GetDefaultDataflowLinkOptions());
            broadcastReplay.LinkTo(joinStoreUgcFile.Target2, GetDefaultDataflowLinkOptions());
            joinStoreUgcFile.LinkTo(storeUgcFile, GetDefaultDataflowLinkOptions());

            getUgcFile.Completion.ContinueWith(t =>
            {
                downloadNotifier.Dispose();
            }, cancellationToken);
            storeUgcFile.Completion.ContinueWith(t =>
            {
                storeNotifier.Dispose();
            }, cancellationToken);
        }

        readonly ISteamWebApiClient steamWebApiClient;
        readonly uint appId;
        readonly IUgcHttpClient ugcHttpClient;
        readonly ICloudBlobDirectory directory;
        readonly CancellationToken cancellationToken;
        DownloadNotifier downloadNotifier;
        StoreNotifier storeNotifier;

        readonly BroadcastBlock<long> broadcastUgcId;
        readonly BufferBlock<Replay> bufferReplay;
        readonly TransformBlock<Tuple<ReplayData, Replay>, Uri> storeUgcFile;

        public ISourceBlock<Replay> DownloadReplay { get => bufferReplay; }
        public ISourceBlock<Uri> StoreReplayFile { get => storeUgcFile; }

        /// <summary>
        /// Posts an item to the <see cref="ReplayDataflowNetwork"/>.
        /// </summary>
        /// <param name="id">The UGC ID of the replay.</param>
        /// <returns>
        /// true if the item was accepted; otherwise, false.
        /// </returns>
        public bool Post(long id) => broadcastUgcId.Post(id);

        /// <summary>
        /// Signals to the <see cref="ReplayDataflowNetwork"/> that it should 
        /// not accept nor produce any more messages nor consume any more postponed messages.
        /// </summary>
        public void Complete() => broadcastUgcId.Complete();

        internal Task<UgcFileDetailsEnvelope> GetUgcFileDetailsAsync(long ugcId)
        {
            if (downloadNotifier == null) { downloadNotifier = new DownloadNotifier(Log, "replays"); }

            return steamWebApiClient.GetUgcFileDetailsAsync(appId, ugcId, downloadNotifier, cancellationToken);
        }

        internal Task<byte[]> GetUgcFileAsync(UgcFileDetailsEnvelope ugcFileDetails)
        {
            return ugcHttpClient.GetUgcFileAsync(ugcFileDetails.Data.Url, downloadNotifier, cancellationToken);
        }

        internal static ReplayData ReadReplayData(byte[] rawReplayData)
        {
            using (var ms = new MemoryStream(rawReplayData))
            {
                return ReplaySerializer.Deserialize(ms);
            }
        }

        internal static Replay CreateReplay(long ugcId, ReplayData replayData)
        {
            var replay = new Replay { ReplayId = ugcId };

            replay.Version = replayData.Header.Version;
            replay.KilledBy = replayData.Header.KilledBy;
            if (replayData.TryGetSeed(out int seed))
            {
                replay.Seed = seed;
            }

            return replay;
        }

        internal async Task<Uri> StoreUgcFileAsync(ReplayData replayData, Replay replay)
        {
            if (storeNotifier == null) { storeNotifier = new StoreNotifier(Log, "replay files"); }

            using (var ugcFile = new MemoryStream())
            {
                ReplaySerializer.Serialize(ugcFile, replayData);
                ugcFile.Position = 0;

                var blob = directory.GetBlockBlobReference(replay.FileName);
                blob.Properties.ContentType = "application/octet-stream";
                blob.Properties.CacheControl = "max-age=604800"; // 1 week

                await blob.UploadFromStreamAsync(ugcFile, cancellationToken).ConfigureAwait(false);

                Log.Debug(blob.Uri);
                storeNotifier.Report(1);

                return blob.Uri;
            }
        }

        DataflowBlockOptions GetDefaultDataflowBlockOptions() => new DataflowBlockOptions
        {
            CancellationToken = cancellationToken,
        };

        GroupingDataflowBlockOptions GetDefaultGroupingDataflowBlockOptions() => new GroupingDataflowBlockOptions
        {
            CancellationToken = cancellationToken,
        };

        ExecutionDataflowBlockOptions GetNetworkBoundExecutionDataflowBlockOptions() => new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 8,
            CancellationToken = cancellationToken,
        };

        ExecutionDataflowBlockOptions GetProcessorBoundExecutionDataflowBlockOptions() => new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken,
        };

        DataflowLinkOptions GetDefaultDataflowLinkOptions() => new DataflowLinkOptions
        {
            PropagateCompletion = true,
        };
    }
}
