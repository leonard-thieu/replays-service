using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using log4net;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamRemoteStorage;
using toofz.NecroDancer.Leaderboards.Steam.Workshop;
using toofz.NecroDancer.Replays;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal sealed class ReplayDataflowNetwork
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ReplayDataflowNetwork));

        private static readonly ReplayDataSerializer ReplayDataSerializer = new ReplayDataSerializer();

        public ReplayDataflowNetwork(
            uint appId,
            ISteamWebApiClient steamWebApiClient,
            IUgcHttpClient ugcHttpClient,
            ICloudBlobDirectory directory,
            CancellationToken cancellationToken)
        {
            this.appId = appId;
            this.steamWebApiClient = steamWebApiClient;
            this.ugcHttpClient = ugcHttpClient;
            this.directory = directory;
            this.cancellationToken = cancellationToken;

            getReplayDataflowContext = new TransformBlock<Replay, ReplayDataflowContext>(
                replay => new ReplayDataflowContext(replay),
                GetProcessorBoundExecutionDataflowBlockOptions());

            var getUgcFileDetails = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                (Func<ReplayDataflowContext, Task<ReplayDataflowContext>>)GetUgcFileDetailsAsync,
                GetNetworkBoundExecutionDataflowBlockOptions());
            var getUgcFile = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                (Func<ReplayDataflowContext, Task<ReplayDataflowContext>>)GetUgcFileAsync,
                GetNetworkBoundExecutionDataflowBlockOptions());

            var readReplayData = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                (Func<ReplayDataflowContext, ReplayDataflowContext>)ReadReplayData,
                GetProcessorBoundExecutionDataflowBlockOptions());

            var updateReplay = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                (Func<ReplayDataflowContext, ReplayDataflowContext>)UpdateReplay,
                GetProcessorBoundExecutionDataflowBlockOptions());
            var onUgcFileDetailsError = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                (Func<ReplayDataflowContext, ReplayDataflowContext>)OnUgcFileDetailsError,
                GetProcessorBoundExecutionDataflowBlockOptions());
            var onUgcFileError = new TransformBlock<ReplayDataflowContext, ReplayDataflowContext>(
                (Func<ReplayDataflowContext, ReplayDataflowContext>)OnUgcFileError,
                GetProcessorBoundExecutionDataflowBlockOptions());

            var broadcastReplayDataflowContext = new BroadcastBlock<ReplayDataflowContext>(context => context, GetDefaultDataflowBlockOptions());

            var storeUgcFile = new ActionBlock<ReplayDataflowContext>(
                StoreUgcFileAsync,
                GetNetworkBoundExecutionDataflowBlockOptions());

            getReplayDataflowContext.LinkTo(getUgcFileDetails, GetDefaultDataflowLinkOptions());

            getUgcFileDetails.LinkTo(getUgcFile, GetDefaultDataflowLinkOptions(), context => context.UgcFileDetails != null);

            getUgcFile.LinkTo(readReplayData, GetDefaultDataflowLinkOptions(), context => context.UgcFile != null);
            readReplayData.LinkTo(updateReplay, GetDefaultDataflowLinkOptions());
            updateReplay.LinkTo(broadcastReplayDataflowContext);

            getUgcFileDetails.LinkTo(onUgcFileDetailsError, GetDefaultDataflowLinkOptions(), context => context.UgcFileDetailsException != null);
            onUgcFileDetailsError.LinkTo(broadcastReplayDataflowContext);

            getUgcFile.LinkTo(onUgcFileError, GetDefaultDataflowLinkOptions(), context => context.UgcFileException != null);
            onUgcFileError.LinkTo(broadcastReplayDataflowContext);

            var createReplayCompletions = Task.WhenAll(
                updateReplay.Completion,
                onUgcFileDetailsError.Completion,
                onUgcFileError.Completion);
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

            broadcastReplayDataflowContext.LinkTo(storeUgcFile, GetDefaultDataflowLinkOptions(), context => context.ReplayData != null);

            getUgcFile.Completion.ContinueWith(t =>
            {
                if (downloadActivity.IsValueCreated)
                {
                    downloadActivity.Value.Dispose();
                }
            }, cancellationToken);
            storeUgcFile.Completion.ContinueWith(t =>
            {
                if (storeActivity.IsValueCreated)
                {
                    storeActivity.Value.Dispose();
                }
            }, cancellationToken);

            Completion = Task.WhenAll(broadcastReplayDataflowContext.Completion, storeUgcFile.Completion);
        }

        private readonly uint appId;
        private readonly ISteamWebApiClient steamWebApiClient;
        private readonly IUgcHttpClient ugcHttpClient;
        private readonly ICloudBlobDirectory directory;
        private readonly CancellationToken cancellationToken;
        private readonly Lazy<DownloadActivity> downloadActivity = new Lazy<DownloadActivity>(() => new DownloadActivity(Log, "replays"));
        private readonly Lazy<StoreActivity> storeActivity = new Lazy<StoreActivity>(() => new StoreActivity(Log, "replay files"));

        private readonly TransformBlock<Replay, ReplayDataflowContext> getReplayDataflowContext;

        /// <summary>
        /// Gets a <see cref="Task"/> that represents the asynchronous operation and completion of the dataflow block.
        /// </summary>
        public Task Completion { get; }

        /// <summary>
        /// Asynchronously offers a message to the target message block, allowing for postponement.
        /// </summary>
        /// <param name="replay">The replay.</param>
        /// <param name="cancellationToken">
        /// The cancellation token with which to request cancellation of the send operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> that represents the asynchronous send.
        /// If the target accepts and consumes the offered element during the call to SendAsync,
        /// upon return from the call the resulting <see cref="Task{Boolean}"/>
        /// will be completed and its Result property will return true. If the target declines
        /// the offered element during the call, upon return from the call the resulting
        /// <see cref="Task{Boolean}"/> will be completed and its Result property
        /// will return false. If the target postpones the offered element, the element will
        /// be buffered until such time that the target consumes or releases it, at which
        /// point the Task will complete, with its Result indicating whether the message
        /// was consumed. If the target never attempts to consume or release the message,
        /// the returned task will never complete. If cancellation is requested before the
        /// target has successfully consumed the sent data, the returned task will complete
        /// in the Canceled state and the data will no longer be available to the target.
        /// </returns>
        public Task<bool> SendAsync(Replay replay, CancellationToken cancellationToken) => getReplayDataflowContext.SendAsync(replay, cancellationToken);

        /// <summary>
        /// Signals to the <see cref="ReplayDataflowNetwork"/> that it should 
        /// not accept nor produce any more messages nor consume any more postponed messages.
        /// </summary>
        public void Complete() => getReplayDataflowContext.Complete();

        internal async Task<ReplayDataflowContext> GetUgcFileDetailsAsync(ReplayDataflowContext context)
        {
            try
            {
                context.UgcFileDetails = await steamWebApiClient.GetUgcFileDetailsAsync(appId, context.Replay.ReplayId, downloadActivity.Value, cancellationToken);
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
                context.UgcFile = await ugcHttpClient.GetUgcFileAsync(context.UgcFileDetails.Data.Url, downloadActivity.Value, cancellationToken);
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
                context.ReplayData = ReplayDataSerializer.Deserialize(ms);

                return context;
            }
        }

        internal static ReplayDataflowContext UpdateReplay(ReplayDataflowContext context)
        {
            var replay = context.Replay;

            var replayData = context.ReplayData;
            replay.Version = replayData.Version;
            replay.KilledBy = replayData.KilledBy;
            replay.Seed = replayData.Seed;

            return context;
        }

        internal static ReplayDataflowContext OnUgcFileDetailsError(ReplayDataflowContext context)
        {
            var replay = context.Replay;

            replay.ErrorCode = 1000 + (int)context.UgcFileDetailsException.StatusCode;

            return context;
        }

        internal static ReplayDataflowContext OnUgcFileError(ReplayDataflowContext context)
        {
            var replay = context.Replay;

            replay.ErrorCode = 2000 + (int)context.UgcFileException.StatusCode;

            return context;
        }

        internal async Task StoreUgcFileAsync(ReplayDataflowContext context)
        {
            using (var ugcFile = new MemoryStream())
            {
                ReplayDataSerializer.Serialize(ugcFile, context.ReplayData);
                ugcFile.Position = 0;

                var replay = context.Replay;
                var fileName = replay.Version != null ?
                    $"{replay.Version}_{replay.ReplayId}.dat" :
                    $"UNKNOWN_{replay.ReplayId}.dat";
                var blob = directory.GetBlockBlobReference(fileName);
                blob.Properties.ContentType = "application/octet-stream";
                blob.Properties.CacheControl = "max-age=604800"; // 1 week

                await blob.UploadFromStreamAsync(ugcFile, cancellationToken).ConfigureAwait(false);

                context.Replay.Uri = blob.Uri.ToString();
                storeActivity.Value.Report(1);
            }
        }

        private DataflowBlockOptions GetDefaultDataflowBlockOptions() => new DataflowBlockOptions
        {
            CancellationToken = cancellationToken,
        };

        private ExecutionDataflowBlockOptions GetNetworkBoundExecutionDataflowBlockOptions() => new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 8,
            CancellationToken = cancellationToken,
        };

        private ExecutionDataflowBlockOptions GetProcessorBoundExecutionDataflowBlockOptions() => new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken,
        };

        private DataflowLinkOptions GetDefaultDataflowLinkOptions() => new DataflowLinkOptions
        {
            PropagateCompletion = true,
        };

        internal sealed class ReplayDataflowContext
        {
            public ReplayDataflowContext(Replay replay)
            {
                Replay = replay;
            }

            public Replay Replay { get; }
            public UgcFileDetailsEnvelope UgcFileDetails { get; set; }
            public HttpRequestStatusException UgcFileDetailsException { get; set; }
            public byte[] UgcFile { get; set; }
            public HttpRequestStatusException UgcFileException { get; set; }
            public ReplayData ReplayData { get; set; }
        }
    }
}
