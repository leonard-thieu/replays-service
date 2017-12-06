using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using toofz.Data;
using toofz.Steam.WebApi;
using toofz.Steam.WebApi.ISteamRemoteStorage;
using toofz.Steam.Workshop;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    public class ReplaysWorkerTests
    {
        public ReplaysWorkerTests()
        {
            worker = new ReplaysWorker(
                appId,
                mockDb.Object,
                mockSteamWebApiClient.Object,
                mockUgcHttpClient.Object,
                mockDirectoryFactory.Object,
                mockStoreClient.Object,
                telemetryClient);
        }

        private readonly uint appId = 247080;
        private readonly Mock<ILeaderboardsContext> mockDb = new Mock<ILeaderboardsContext>();
        private readonly Mock<ISteamWebApiClient> mockSteamWebApiClient = new Mock<ISteamWebApiClient>();
        private readonly Mock<IUgcHttpClient> mockUgcHttpClient = new Mock<IUgcHttpClient>();
        private readonly Mock<ICloudBlobDirectoryFactory> mockDirectoryFactory = new Mock<ICloudBlobDirectoryFactory>();
        private readonly Mock<ILeaderboardsStoreClient> mockStoreClient = new Mock<ILeaderboardsStoreClient>();
        private readonly TelemetryClient telemetryClient = new TelemetryClient();
        private readonly ReplaysWorker worker;

        public class GetReplaysAsyncMethod : ReplaysWorkerTests
        {
            private readonly CancellationToken cancellationToken = CancellationToken.None;

            [Fact]
            public async Task ReturnsReplays()
            {
                // Arrange
                var dbReplaysInner = new List<Replay>();
                var dbReplays = new FakeDbSet<Replay>(dbReplaysInner);
                mockDb.Setup(d => d.Replays).Returns(dbReplays);
                var limit = 20;

                // Act
                var replays = await worker.GetReplaysAsync(limit, cancellationToken);

                // Assert
                Assert.IsAssignableFrom<IEnumerable<Replay>>(replays);
            }
        }

        public class UpdateReplaysAsyncMethod : ReplaysWorkerTests
        {
            public UpdateReplaysAsyncMethod()
            {
                var mockBlob = new Mock<ICloudBlockBlob>();
                mockBlob.SetupGet(b => b.Properties).Returns(new BlobProperties());
                mockBlob.SetupGet(b => b.Uri).Returns(new Uri("http://example.org/"));

                var mockDirectory = new Mock<ICloudBlobDirectory>();
                mockDirectory.Setup(d => d.GetBlockBlobReference(It.IsAny<string>())).Returns(mockBlob.Object);

                mockDirectoryFactory.Setup(f => f.GetCloudBlobDirectoryAsync("replays", cancellationToken)).ReturnsAsync(mockDirectory.Object);
            }

            private readonly List<Replay> replays = new List<Replay>();
            private readonly CancellationToken cancellationToken = CancellationToken.None;


            [Fact]
            public async Task UpdatesReplays()
            {
                // Arrange
                mockSteamWebApiClient
                    .Setup(c => c.GetUgcFileDetailsAsync(appId, It.IsAny<long>(), It.IsAny<IProgress<long>>(), cancellationToken))
                    .ReturnsAsync(new UgcFileDetailsEnvelope { Data = new UgcFileDetails() });
                replays.Add(new Replay());

                // Act
                await worker.UpdateReplaysAsync(replays, cancellationToken);

                // Assert
                Assert.IsAssignableFrom<IEnumerable<Replay>>(replays);
            }
        }

        public class StoreReplaysAsyncMethod : ReplaysWorkerTests
        {
            private readonly List<Replay> replays = new List<Replay>();
            private readonly CancellationToken cancellationToken = CancellationToken.None;


            [Fact]
            public async Task StoresReplays()
            {
                // Arrange
                mockStoreClient
                    .Setup(c => c.BulkUpsertAsync(replays, null, cancellationToken))
                    .ReturnsAsync(replays.Count);

                // Act
                await worker.StoreReplaysAsync(replays, cancellationToken);

                // Assert
                mockStoreClient.Verify(c => c.BulkUpsertAsync(replays, null, cancellationToken), Times.Once);
            }
        }
    }
}
