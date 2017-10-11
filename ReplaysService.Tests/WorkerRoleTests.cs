using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    class WorkerRoleTests
    {
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
