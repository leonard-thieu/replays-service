using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.toofz;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    class WorkerRoleTests
    {
        [TestClass]
        public class UpdateReplaysAsync
        {
            public UpdateReplaysAsync()
            {
                directory = WorkerRole.GetDirectory("UseDevelopmentStorage=true");
            }

            CloudBlobDirectory directory;

            [TestMethod]
            [TestCategory("Integration")]
            public async Task ApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var workerRole = new WorkerRole();

                var mockISteamWebApiClient = new Mock<ISteamWebApiClient>();
                var mockIUgcHttpClient = new Mock<IUgcHttpClient>();

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdateReplaysAsync(
                        null,
                        mockISteamWebApiClient.Object,
                        mockIUgcHttpClient.Object,
                        directory,
                        1);
                });
            }

            [TestMethod]
            [TestCategory("Integration")]
            public async Task SteamWebApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var workerRole = new WorkerRole();

                var mockIToofzApiClient = new Mock<IToofzApiClient>();
                var mockIUgcHttpClient = new Mock<IUgcHttpClient>();

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdateReplaysAsync(
                        mockIToofzApiClient.Object,
                        null,
                        mockIUgcHttpClient.Object,
                        directory,
                        1);
                });
            }

            [TestMethod]
            [TestCategory("Integration")]
            public async Task UgcHttpClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var workerRole = new WorkerRole();

                var mockIToofzApiClient = new Mock<IToofzApiClient>();
                var mockISteamWebApiClient = new Mock<ISteamWebApiClient>();

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdateReplaysAsync(
                        mockIToofzApiClient.Object,
                        mockISteamWebApiClient.Object,
                        null,
                        directory,
                        1);
                });
            }

            [TestMethod]
            public async Task DirectoryIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var workerRole = new WorkerRole();

                var mockIToofzApiClient = new Mock<IToofzApiClient>();
                var mockISteamWebApiClient = new Mock<ISteamWebApiClient>();
                var mockIUgcHttpClient = new Mock<IUgcHttpClient>();

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                {
                    return workerRole.UpdateReplaysAsync(
                        mockIToofzApiClient.Object,
                        mockISteamWebApiClient.Object,
                        mockIUgcHttpClient.Object,
                        null,
                        1);
                });
            }

            [TestMethod]
            [TestCategory("Integration")]
            public async Task LimitIsNegative_ThrowsArgumentOutOfRangeException()
            {
                // Arrange
                var workerRole = new WorkerRole();

                var mockIToofzApiClient = new Mock<IToofzApiClient>();
                var mockISteamWebApiClient = new Mock<ISteamWebApiClient>();
                var mockIUgcHttpClient = new Mock<IUgcHttpClient>();

                // Act -> Assert
                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                {
                    return workerRole.UpdateReplaysAsync(
                        mockIToofzApiClient.Object,
                        mockISteamWebApiClient.Object,
                        mockIUgcHttpClient.Object,
                        directory,
                        -1);
                });
            }

            [TestMethod]
            [TestCategory("Integration")]
            public async Task UpdatesReplays()
            {
                // Arrange
                var workerRole = new WorkerRole();

                var mockIToofzApiClient = new Mock<IToofzApiClient>();
                mockIToofzApiClient
                    .Setup(toofzApiClient => toofzApiClient.GetReplaysAsync(It.IsAny<GetReplaysParams>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new Replays()));
                mockIToofzApiClient
                    .Setup(toofzApiClient => toofzApiClient.PostReplaysAsync(It.IsAny<IEnumerable<Replay>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new BulkStore()));

                var mockISteamWebApiClient = new Mock<ISteamWebApiClient>();
                var mockIUgcHttpClient = new Mock<IUgcHttpClient>();

                // Act -> Assert
                await workerRole.UpdateReplaysAsync(
                    mockIToofzApiClient.Object,
                    mockISteamWebApiClient.Object,
                    mockIUgcHttpClient.Object,
                    directory,
                    1);
            }
        }
    }
}
