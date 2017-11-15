using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.Services;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    public class WorkerRoleTests
    {
        private readonly TelemetryClient telemetryClient = new TelemetryClient();
        private readonly IReplaysSettings settings = new StubReplaysSettings();

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
                // Arrange 
                var telemetryClient = new TelemetryClient();

                // Act
                var handler = WorkerRole.CreateSteamWebApiHandler(telemetryClient);

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
                blobClient = mockBlobClient.Object;
                container = mockContainer.Object;

                mockBlobClient.Setup(c => c.GetContainerReference("crypt")).Returns(container);
                mockContainer.Setup(c => c.GetDirectoryReference("replays")).Returns(Mock.Of<ICloudBlobDirectory>());
            }

            private Mock<ICloudBlobClient> mockBlobClient = new Mock<ICloudBlobClient>();
            private ICloudBlobClient blobClient;
            private Mock<ICloudBlobContainer> mockContainer = new Mock<ICloudBlobContainer>();
            private ICloudBlobContainer container;
            private CancellationToken cancellationToken = CancellationToken.None;

            [Fact]
            public async Task ContainerDoesNotExist_CreatesContainer()
            {
                // Arrange
                mockContainer.Setup(c => c.ExistsAsync(cancellationToken)).ReturnsAsync(false);

                // Act
                await WorkerRole.GetCloudBlobDirectory(blobClient, cancellationToken);

                // Assert
                mockContainer.Verify(c => c.CreateAsync(cancellationToken), Times.Once);
            }

            [Fact]
            public async Task ContainerExists_DoesNotCreateContainer()
            {
                // Arrange
                mockContainer.Setup(c => c.ExistsAsync(cancellationToken)).ReturnsAsync(true);

                // Act
                await WorkerRole.GetCloudBlobDirectory(blobClient, cancellationToken);

                // Assert
                mockContainer.Verify(c => c.CreateAsync(cancellationToken), Times.Never);
            }

            [Fact]
            public async Task SetsPermissionsToPublic()
            {
                // Arrange -> Act
                await WorkerRole.GetCloudBlobDirectory(blobClient, cancellationToken);

                // Assert
                mockContainer.Verify(
                    c => c.SetPermissionsAsync(
                        It.Is<BlobContainerPermissions>(p => p.PublicAccess == BlobContainerPublicAccessType.Blob),
                        cancellationToken));
            }

            [Fact]
            public async Task ReturnsDirectory()
            {
                // Arrange -> Act
                var directory = await WorkerRole.GetCloudBlobDirectory(blobClient, cancellationToken);

                // Assert
                Assert.IsAssignableFrom<ICloudBlobDirectory>(directory);
            }
        }

        public class OnStartMethod : WorkerRoleTests
        {
            public OnStartMethod()
            {
                settings.ToofzApiUserName = "myUserName";
                settings.ToofzApiPassword = new EncryptedSecret("myPassword", 1);
                workerRole = new WorkerRole(settings, telemetryClient);
            }

            private readonly WorkerRole workerRole;

            [Fact]
            public void ToofzApiUserNameIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                settings.ToofzApiUserName = null;

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
                settings.ToofzApiUserName = "";

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
                settings.ToofzApiPassword = null;

                // Act -> Assert
                Assert.Throws<InvalidOperationException>(() =>
                {
                    workerRole.Start();
                });
            }
        }

        public class RunAsyncOverrideMethod : WorkerRoleTests
        {
            public RunAsyncOverrideMethod()
            {
                settings.ToofzApiBaseAddress = "http://example.org";
                settings.SteamWebApiKey = new EncryptedSecret("mySteamWebApiKey", 1);
                settings.AzureStorageConnectionString = new EncryptedSecret("myAzureStorageConnectionString", 1);
                settings.ReplaysPerUpdate = 1;
                workerRole = new WorkerRoleAdapter(settings, telemetryClient);
            }

            private readonly WorkerRoleAdapter workerRole;
            private readonly CancellationToken cancellationToken = default;

            [Fact]
            public async Task ToofzApiBaseAddressIsNull_ThrowsInvalidOperationException()
            {
                // Arrange
                settings.ToofzApiBaseAddress = null;

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
                settings.ToofzApiBaseAddress = "";

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
                settings.SteamWebApiKey = null;

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
                settings.AzureStorageConnectionString = null;

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
                settings.ReplaysPerUpdate = 0;

                // Act -> Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                {
                    return workerRole.PublicRunAsyncOverride(cancellationToken);
                });
            }

            private class WorkerRoleAdapter : WorkerRole
            {
                public WorkerRoleAdapter(IReplaysSettings settings, TelemetryClient telemetryClient) : base(settings, telemetryClient) { }

                public Task PublicRunAsyncOverride(CancellationToken cancellationToken) => RunAsyncOverride(cancellationToken);
            }
        }

        private class StubReplaysSettings : IReplaysSettings
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
