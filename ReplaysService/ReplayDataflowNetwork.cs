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

            getReplayDataflowContext = new TransformBlock<long, ReplayDataflowContext>(
                ugcId => new ReplayDataflowContext(ugcId),
                GetProcessorBoundExecutionDataflowBlockOptions());

            var getUgcFileDetails = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                context => GetUgcFileDetailsAsync(context),
                GetNetworkBoundExecutionDataflowBlockOptions());
            var getUgcFile = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                context => GetUgcFileAsync(context),
                GetNetworkBoundExecutionDataflowBlockOptions());

            var readReplayData = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                context => ReadReplayData(context),
                GetProcessorBoundExecutionDataflowBlockOptions());

            var createReplay = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                context => CreateReplay(context),
                GetProcessorBoundExecutionDataflowBlockOptions());
            var createReplayWithoutUgcFileDetails = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                context => CreateReplayWithoutUgcFileDetails(context),
                GetProcessorBoundExecutionDataflowBlockOptions());
            var createReplayWithoutUgcFile = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                context => CreateReplayWithoutUgcFile(context),
                GetProcessorBoundExecutionDataflowBlockOptions());

            var broadcastReplayDataflowContext = new BroadcastBlock<ReplayDataflowContext>(context => context, GetDefaultDataflowBlockOptions());
            getReplay = new TransformBlock<ReplayDataflowContext, Replay>(context => GetReplay(context), GetProcessorBoundExecutionDataflowBlockOptions());

            storeUgcFile = new TransformBlock<ReplayDataflowContext, Uri>(
                context => StoreUgcFileAsync(context),
                GetNetworkBoundExecutionDataflowBlockOptions());

            getReplayDataflowContext.LinkTo(getUgcFileDetails, GetDefaultDataflowLinkOptions());

            getUgcFileDetails.LinkTo(getUgcFile, GetDefaultDataflowLinkOptions(), context => context.UgcFileDetails != null);

            getUgcFile.LinkTo(readReplayData, GetDefaultDataflowLinkOptions(), context => context.UgcFile != null);
            readReplayData.LinkTo(createReplay, GetDefaultDataflowLinkOptions());
            createReplay.LinkTo(broadcastReplayDataflowContext);

            getUgcFileDetails.LinkTo(createReplayWithoutUgcFileDetails, GetDefaultDataflowLinkOptions(), context => context.UgcFileException != null);
            createReplayWithoutUgcFileDetails.LinkTo(broadcastReplayDataflowContext);

            getUgcFile.LinkTo(createReplayWithoutUgcFile, GetDefaultDataflowLinkOptions(), context => context.UgcFileException != null);
            createReplayWithoutUgcFile.LinkTo(broadcastReplayDataflowContext);

            var createReplayCompletions = Task.WhenAll(
                createReplay.Completion,
                createReplayWithoutUgcFileDetails.Completion,
                createReplayWithoutUgcFile.Completion);
            createReplayCompletions.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    ((IDataflowBlock)broadcastReplayDataflowContext).Fault(t.Exception);
                }
                else
                {
                    broadcastReplayDataflowContext.Complete();
                }
            }, cancellationToken);

            broadcastReplayDataflowContext.LinkTo(getReplay, GetDefaultDataflowLinkOptions());

            broadcastReplayDataflowContext.LinkTo(storeUgcFile, GetDefaultDataflowLinkOptions(), context => context.ReplayData != null);

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

        readonly TransformBlock<long, ReplayDataflowContext> getReplayDataflowContext;
        readonly TransformBlock<ReplayDataflowContext, Replay> getReplay;
        readonly TransformBlock<ReplayDataflowContext, Uri> storeUgcFile;

        public ISourceBlock<Replay> DownloadReplay { get => getReplay; }
        public ISourceBlock<Uri> StoreReplayFile { get => storeUgcFile; }

        /// <summary>
        /// Asynchronously offers a message to the target message block, allowing for postponement.
        /// </summary>
        /// <param name="id">The UGC ID of the replay.</param>
        /// <param name="cancellationToken">
        /// The cancellation token with which to request cancellation of the send operation.
        /// </param>
        /// <returns>
        ///  A <see cref="Task{Boolean}"/> that represents the asynchronous send.
        ///  If the target accepts and consumes the offered element during the call to SendAsync,
        ///  upon return from the call the resulting <see cref="Task{Boolean}"/>
        ///  will be completed and its Result property will return true. If the target declines
        ///  the offered element during the call, upon return from the call the resulting
        ///  <see cref="Task{Boolean}"/> will be completed and its Result property
        ///  will return false. If the target postpones the offered element, the element will
        ///  be buffered until such time that the target consumes or releases it, at which
        ///  point the Task will complete, with its Result indicating whether the message
        ///  was consumed. If the target never attempts to consume or release the message,
        ///  the returned task will never complete. If cancellation is requested before the
        ///  target has successfully consumed the sent data, the returned task will complete
        ///  in the Canceled state and the data will no longer be available to the target.
        /// </returns>
        public Task<bool> SendAsync(long id, CancellationToken cancellationToken) => getReplayDataflowContext.SendAsync(id, cancellationToken);

        /// <summary>
        /// Signals to the <see cref="ReplayDataflowNetwork"/> that it should 
        /// not accept nor produce any more messages nor consume any more postponed messages.
        /// </summary>
        public void Complete() => getReplayDataflowContext.Complete();

        internal async Task<ReplayDataflowContext> GetUgcFileDetailsAsync(ReplayDataflowContext context)
        {
            if (downloadNotifier == null) { downloadNotifier = new DownloadNotifier(Log, "replays"); }

            try
            {
                context.UgcFileDetails = await steamWebApiClient.GetUgcFileDetailsAsync(appId, context.UgcId, downloadNotifier, cancellationToken);
            }
            catch (HttpRequestStatusException ex)
            {
                context.UgcFileDetailsException = ex;
            }

            return context;
        }

        internal async Task<ReplayDataflowContext> GetUgcFileAsync(ReplayDataflowContext context)
        {
            try
            {
                context.UgcFile = await ugcHttpClient.GetUgcFileAsync(context.UgcFileDetails.Data.Url, downloadNotifier, cancellationToken);
            }
            catch (HttpRequestStatusException ex)
            {
                context.UgcFileException = ex;
            }

            return context;
        }

        internal static ReplayDataflowContext ReadReplayData(ReplayDataflowContext context)
        {
            using (var ms = new MemoryStream(context.UgcFile))
            {
                context.ReplayData = ReplaySerializer.Deserialize(ms);

                return context;
            }
        }

        internal static ReplayDataflowContext CreateReplay(ReplayDataflowContext context)
        {
            var replay = new Replay { ReplayId = context.UgcId };

            var replayData = context.ReplayData;
            replay.Version = replayData.Header.Version;
            replay.KilledBy = replayData.Header.KilledBy;
            if (replayData.TryGetSeed(out int seed))
            {
                replay.Seed = seed;
            }

            context.Replay = replay;

            return context;
        }

        internal static ReplayDataflowContext CreateReplayWithoutUgcFileDetails(ReplayDataflowContext context)
        {
            var replay = new Replay { ReplayId = context.UgcId };

            replay.ErrorCode = 1000 + (int)context.UgcFileDetailsException.StatusCode;

            context.Replay = replay;

            return context;
        }

        internal static ReplayDataflowContext CreateReplayWithoutUgcFile(ReplayDataflowContext context)
        {
            var replay = new Replay { ReplayId = context.UgcId };

            replay.ErrorCode = 2000 + (int)context.UgcFileException.StatusCode;

            context.Replay = replay;

            return context;
        }

        internal static Replay GetReplay(ReplayDataflowContext context)
        {
            return context.Replay;
        }

        internal async Task<Uri> StoreUgcFileAsync(ReplayDataflowContext context)
        {
            if (storeNotifier == null) { storeNotifier = new StoreNotifier(Log, "replay files"); }

            using (var ugcFile = new MemoryStream())
            {
                ReplaySerializer.Serialize(ugcFile, context.ReplayData);
                ugcFile.Position = 0;

                var blob = directory.GetBlockBlobReference(context.Replay.FileName);
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

        internal sealed class ReplayDataflowContext
        {
            public ReplayDataflowContext(long ugcId)
            {
                UgcId = ugcId;
            }

            public long UgcId { get; }
            public UgcFileDetailsEnvelope UgcFileDetails { get; set; }
            public HttpRequestStatusException UgcFileDetailsException { get; set; }
            public byte[] UgcFile { get; set; }
            public HttpRequestStatusException UgcFileException { get; set; }
            public ReplayData ReplayData { get; set; }
            public Replay Replay { get; set; }
        }
    }
}
