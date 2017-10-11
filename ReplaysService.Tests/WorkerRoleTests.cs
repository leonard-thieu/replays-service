using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    class WorkerRoleTests
    {
        [TestClass]
        public class CreateToofzApiHandlerMethod
        {
            [TestMethod]
            public void ReturnsHandler()
            {
                // Arrange
                var toofzApiUserName = "myUserName";
                var toofzApiPassword = "myPassword";

                // Act
                var handler = WorkerRole.CreateToofzApiHandler(toofzApiUserName, toofzApiPassword);

                // Assert
                Assert.IsInstanceOfType(handler, typeof(HttpMessageHandler));
            }
        }

        [TestClass]
        public class CreateSteamWebApiHandlerMethod
        {
            [TestMethod]
            public void ReturnsHandler()
            {
                // Arrange -> Act
                var handler = WorkerRole.CreateSteamWebApiHandler();

                // Assert
                Assert.IsInstanceOfType(handler, typeof(HttpMessageHandler));
            }
        }

        [TestClass]
        public class CreateUgcHandlerMethod
        {
            [TestMethod]
            public void ReturnsHandler()
            {
                // Arrange -> Act
                var handler = WorkerRole.CreateUgcHandler();

                // Assert
                Assert.IsInstanceOfType(handler, typeof(HttpMessageHandler));
            }
        }

        [TestClass]
        public class GetCloudBlobDirectoryMethod
        {
            public GetCloudBlobDirectoryMethod()
            {
                BlobClient = MockBlobClient.Object;
                Container = MockContainer.Object;

                MockBlobClient.Setup(c => c.GetContainerReference("crypt")).Returns(Container);
                MockContainer.Setup(c => c.GetDirectoryReference("replays")).Returns(Mock.Of<ICloudBlobDirectory>());
            }

            public Mock<ICloudBlobClient> MockBlobClient { get; set; } = new Mock<ICloudBlobClient>();
            public ICloudBlobClient BlobClient { get; set; }
            public Mock<ICloudBlobContainer> MockContainer { get; set; } = new Mock<ICloudBlobContainer>();
            public ICloudBlobContainer Container { get; set; }
            public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

            [TestMethod]
            public async Task ContainerDoesNotExist_CreatesContainer()
            {
                // Arrange
                MockContainer.Setup(c => c.ExistsAsync(CancellationToken)).ReturnsAsync(false);

                // Act
                await WorkerRole.GetCloudBlobDirectory(BlobClient, CancellationToken);

                // Assert
                MockContainer.Verify(c => c.CreateAsync(CancellationToken), Times.Once);
            }

            [TestMethod]
            public async Task ContainerExists_DoesNotCreateContainer()
            {
                // Arrange
                MockContainer.Setup(c => c.ExistsAsync(CancellationToken)).ReturnsAsync(true);

                // Act
                await WorkerRole.GetCloudBlobDirectory(BlobClient, CancellationToken);

                // Assert
                MockContainer.Verify(c => c.CreateAsync(CancellationToken), Times.Never);
            }

            [TestMethod]
            public async Task SetsPermissionsToPublic()
            {
                // Arrange -> Act
                await WorkerRole.GetCloudBlobDirectory(BlobClient, CancellationToken);

                // Assert
                MockContainer.Verify(c => c.SetPermissionsAsync(It.Is<BlobContainerPermissions>(p => p.PublicAccess == BlobContainerPublicAccessType.Blob), CancellationToken));
            }

            [TestMethod]
            public async Task ReturnsDirectory()
            {
                // Arrange -> Act
                var directory = await WorkerRole.GetCloudBlobDirectory(BlobClient, CancellationToken);

                // Assert
                Assert.IsInstanceOfType(directory, typeof(ICloudBlobDirectory));
            }
        }

        [TestClass]
        public class OnStartMethod
        {
            [TestMethod]
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
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }

            [TestMethod]
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
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }

            [TestMethod]
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
                Assert.ThrowsException<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }
        }

        [TestClass]
        public class RunAsyncOverrideMethod
        {
            [TestMethod]
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
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            [TestMethod]
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
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            [TestMethod]
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
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            [TestMethod]
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
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            [TestMethod]
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
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            class WorkerRoleAdapter : WorkerRole
            {
                public WorkerRoleAdapter(IReplaysSettings settings) : base(settings) { }

                public Task PublicRunAsyncOverride(CancellationToken cancellationToken) => RunAsyncOverride(cancellationToken);
            }
        }
    }
}
