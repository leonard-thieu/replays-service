using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    class WorkerRoleTests
    {
        [TestClass]
        public class UpdateReplaysAsyncMethod
        {
            [TestMethod]
            public async Task ApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var settings = new StubReplaysSettings();
                var workerRole = new WorkerRole(settings);
                IToofzApiClient toofzApiClient = null;
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var limit = 1;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdateReplaysAsync(toofzApiClient, steamWebApiClient, ugcHttpClient, directory, limit);
                });
            }

            [TestMethod]
            public async Task SteamWebApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var settings = new StubReplaysSettings();
                var workerRole = new WorkerRole(settings);
                var toofzApiClient = Mock.Of<IToofzApiClient>();
                ISteamWebApiClient steamWebApiClient = null;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var limit = 1;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdateReplaysAsync(toofzApiClient, steamWebApiClient, ugcHttpClient, directory, limit);
                });
            }

            [TestMethod]
            public async Task UgcHttpClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var settings = new StubReplaysSettings();
                var workerRole = new WorkerRole(settings);
                var toofzApiClient = Mock.Of<IToofzApiClient>();
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                IUgcHttpClient ugcHttpClient = null;
                var directory = Mock.Of<ICloudBlobDirectory>();
                var limit = 1;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdateReplaysAsync(toofzApiClient, steamWebApiClient, ugcHttpClient, directory, limit);
                });
            }

            [TestMethod]
            public async Task DirectoryIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var settings = new StubReplaysSettings();
                var workerRole = new WorkerRole(settings);
                var toofzApiClient = Mock.Of<IToofzApiClient>();
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                ICloudBlobDirectory directory = null;
                var limit = 1;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdateReplaysAsync(toofzApiClient, steamWebApiClient, ugcHttpClient, directory, limit);
                });
            }

            [TestMethod]
            public async Task LimitIsNegative_ThrowsArgumentOutOfRangeException()
            {
                // Arrange
                var settings = new StubReplaysSettings();
                var workerRole = new WorkerRole(settings);
                var toofzApiClient = Mock.Of<IToofzApiClient>();
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var limit = -1;

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                {
                    return workerRole.UpdateReplaysAsync(toofzApiClient, steamWebApiClient, ugcHttpClient, directory, limit);
                });
            }

            [TestMethod]
            public async Task UpdatesReplays()
            {
                // Arrange
                var settings = new StubReplaysSettings();
                var workerRole = new WorkerRole(settings);
                var mockToofzApiClient = new Mock<IToofzApiClient>();
                mockToofzApiClient
                    .Setup(t => t.GetReplaysAsync(It.IsAny<GetReplaysParams>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new ReplaysEnvelope()));
                mockToofzApiClient
                    .Setup(tootzApiClient => tootzApiClient.PostReplaysAsync(It.IsAny<IEnumerable<Replay>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new BulkStoreDTO()));
                var toofzApiClient = mockToofzApiClient.Object;
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var limit = 1;

                // Act -> Assert
                await workerRole.UpdateReplaysAsync(toofzApiClient, steamWebApiClient, ugcHttpClient, directory, limit);
            }
        }

        [TestClass]
        public class DownloadReplaysAndStoreReplayFilesAsyncMethod
        {
            [TestMethod]
            [Ignore]
            public async Task ReturnsReplays()
            {
                // Arrange
                var settings = new StubReplaysSettings();
                var workerRole = new WorkerRole(settings);
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var staleReplays = new List<ReplayDTO>
                {
                    new ReplayDTO(),
                };
                var cancellationToken = CancellationToken.None;

                // Act
                var replays = await workerRole.DownloadReplaysAndStoreReplayFilesAsync(steamWebApiClient, ugcHttpClient, directory, staleReplays, cancellationToken);

                // Assert
                Assert.IsInstanceOfType(replays, typeof(IEnumerable<Replay>));
            }
        }

        [TestClass]
        public class GetStaleReplaysAsyncMethod
        {
            [TestMethod]
            public async Task ReturnsStaleReplays()
            {
                // Arrange
                var mockToofzApiClient = new Mock<IToofzApiClient>();
                mockToofzApiClient
                    .Setup(c => c.GetReplaysAsync(It.IsAny<GetReplaysParams>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new ReplaysEnvelope()));
                var toofzApiClient = mockToofzApiClient.Object;
                var limit = 20;
                var cancellationToken = CancellationToken.None;

                // Act
                var staleReplays = await WorkerRole.GetStaleReplaysAsync(toofzApiClient, limit, cancellationToken);

                // Assert
                Assert.IsInstanceOfType(staleReplays, typeof(IEnumerable<ReplayDTO>));
            }
        }

        [TestClass]
        public class StoreReplaysAsyncMethod
        {
            [TestMethod]
            public async Task StoresReplays()
            {
                // Arrange
                var replays = new List<Replay>();
                var cancellationToken = CancellationToken.None;
                var mockToofzApiClient = new Mock<IToofzApiClient>();
                mockToofzApiClient
                    .Setup(c => c.PostReplaysAsync(replays, cancellationToken))
                    .Returns(Task.FromResult(new BulkStoreDTO()));
                var toofzApiClient = mockToofzApiClient.Object;

                // Act
                await WorkerRole.StoreReplaysAsync(toofzApiClient, replays, cancellationToken);

                // Assert
                mockToofzApiClient.Verify(c => c.PostReplaysAsync(replays, cancellationToken), Times.Once);
            }
        }

        class StubReplaysSettings : IReplaysSettings
        {
            public uint AppId => 247080;

            public int ReplaysPerUpdate { get; set; }
            public string ToofzApiBaseAddress { get; set; }
            public string ToofzApiUserName { get; set; }
            public EncryptedSecret ToofzApiPassword { get; set; }
            public EncryptedSecret SteamWebApiKey { get; set; }
            public EncryptedSecret AzureStorageConnectionString { get; set; }
            public TimeSpan UpdateInterval { get; set; }
            public TimeSpan DelayBeforeGC { get; set; }
            public string InstrumentationKey { get; set; }
            public int KeyDerivationIterations { get; set; }

            public void Reload() { }

            public void Save() { }
        }
    }
}
