using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.Services;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    public class WorkerRoleTests
    {
        public class CreateToofzApiHandlerMethod
        {
            [Fact]
            public void ReturnsHandler()
            {
                // Arrange
                var toofzApiUserName = "myUserName";
                var toofzApiPassword = "myPassword";

                // Act
                var handler = WorkerRole.CreateToofzApiHandler(toofzApiUserName, toofzApiPassword);

                // Assert
                Assert.IsAssignableFrom<HttpMessageHandler>(handler);
            }
        }

        public class CreateSteamWebApiHandlerMethod
        {
            [Fact]
            public void ReturnsHandler()
            {
                // Arrange -> Act
                var handler = WorkerRole.CreateSteamWebApiHandler();

                // Assert
                Assert.IsAssignableFrom<HttpMessageHandler>(handler);
            }
        }

        public class CreateUgcHandlerMethod
        {
            [Fact]
            public void ReturnsHandler()
            {
                // Arrange -> Act
                var handler = WorkerRole.CreateUgcHandler();

                // Assert
                Assert.IsAssignableFrom<HttpMessageHandler>(handler);
            }
        }

        public class GetCloudBlobDirectoryMethod
        {
            public GetCloudBlobDirectoryMethod()
            {
                BlobClient = MockBlobClient.Object;
                Container = MockContainer.Object;

                MockBlobClient.Setup(c => c.GetContainerReference("crypt")).Returns(Container);
                MockContainer.Setup(c => c.GetDirectoryReference("replays")).Returns(Mock.Of<ICloudBlobDirectory>());
            }

            internal Mock<ICloudBlobClient> MockBlobClient { get; set; } = new Mock<ICloudBlobClient>();
            internal ICloudBlobClient BlobClient { get; set; }
            internal Mock<ICloudBlobContainer> MockContainer { get; set; } = new Mock<ICloudBlobContainer>();
            internal ICloudBlobContainer Container { get; set; }
            public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

            [Fact]
            public async Task ContainerDoesNotExist_CreatesContainer()
            {
                // Arrange
                MockContainer.Setup(c => c.ExistsAsync(CancellationToken)).ReturnsAsync(false);

                // Act
                await WorkerRole.GetCloudBlobDirectory(BlobClient, CancellationToken);

                // Assert
                MockContainer.Verify(c => c.CreateAsync(CancellationToken), Times.Once);
            }

            [Fact]
            public async Task ContainerExists_DoesNotCreateContainer()
            {
                // Arrange
                MockContainer.Setup(c => c.ExistsAsync(CancellationToken)).ReturnsAsync(true);

                // Act
                await WorkerRole.GetCloudBlobDirectory(BlobClient, CancellationToken);

                // Assert
                MockContainer.Verify(c => c.CreateAsync(CancellationToken), Times.Never);
            }

            [Fact]
            public async Task SetsPermissionsToPublic()
            {
                // Arrange -> Act
                await WorkerRole.GetCloudBlobDirectory(BlobClient, CancellationToken);

                // Assert
                MockContainer.Verify(c => c.SetPermissionsAsync(It.Is<BlobContainerPermissions>(p => p.PublicAccess == BlobContainerPublicAccessType.Blob), CancellationToken));
            }

            [Fact]
            public async Task ReturnsDirectory()
            {
                // Arrange -> Act
                var directory = await WorkerRole.GetCloudBlobDirectory(BlobClient, CancellationToken);

                // Assert
                Assert.IsAssignableFrom<ICloudBlobDirectory>(directory);
            }
        }

        public class OnStartMethod
        {
            [Fact]
            public void ToofzApiUserNameIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubReplaysSettings
                {
                    ToofzApiUserName = null,
                    ToofzApiPassword = new EncryptedSecret("a", 1),
                };
                var workerRole = new WorkerRole(settings);

                // Act -> Assert
                Assert.Throws<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }

            [Fact]
            public void ToofzApiUserNameIsEmpty_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubReplaysSettings
                {
                    ToofzApiUserName = "",
                    ToofzApiPassword = new EncryptedSecret("a", 1),
                };
                var workerRole = new WorkerRole(settings);

                // Act -> Assert
                Assert.Throws<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }

            [Fact]
            public void ToofzApiPasswordIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = null,
                };
                var workerRole = new WorkerRole(settings);

                // Act -> Assert
                Assert.Throws<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }
        }

        public class RunAsyncOverrideMethod
        {
            [Fact]
            public async Task ToofzApiBaseAddressIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubReplaysSettings
                {
                    ToofzApiBaseAddress = null,
                    SteamWebApiKey = new EncryptedSecret("a", 1),
                    AzureStorageConnectionString = new EncryptedSecret("a", 1),
                };
                var workerRole = new WorkerRoleAdapter(settings);
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            [Fact]
            public async Task ToofzApiBaseAddressIsEmpty_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubReplaysSettings
                {
                    ToofzApiBaseAddress = "",
                    SteamWebApiKey = new EncryptedSecret("a", 1),
                    AzureStorageConnectionString = new EncryptedSecret("a", 1),
                };
                var workerRole = new WorkerRoleAdapter(settings);
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            [Fact]
            public async Task SteamWebApiKeyIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubReplaysSettings
                {
                    ToofzApiBaseAddress = "a",
                    SteamWebApiKey = null,
                    AzureStorageConnectionString = new EncryptedSecret("a", 1),
                };
                var workerRole = new WorkerRoleAdapter(settings);
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            [Fact]
            public async Task AzureStorageConnectionStringIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubReplaysSettings
                {
                    ToofzApiBaseAddress = "a",
                    SteamWebApiKey = new EncryptedSecret("a", 1),
                    AzureStorageConnectionString = null,
                };
                var workerRole = new WorkerRoleAdapter(settings);
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            [Fact]
            public async Task ReplaysPerUpdateIsNotPositive_ThrowsInvalidOperationException()
            {
                // Arrange
                var settings = new StubReplaysSettings
                {
                    ToofzApiBaseAddress = "a",
                    SteamWebApiKey = new EncryptedSecret("a", 1),
                    AzureStorageConnectionString = new EncryptedSecret("a", 1),
                    ReplaysPerUpdate = 0,
                };
                var workerRole = new WorkerRoleAdapter(settings);
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            private class WorkerRoleAdapter : WorkerRole
            {
                public WorkerRoleAdapter(IReplaysSettings settings) : base(settings) { }

                public Task PublicRunAsyncOverride(CancellationToken cancellationToken) => RunAsyncOverride(cancellationToken);
            }
        }
    }
}
