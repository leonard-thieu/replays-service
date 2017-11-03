using System;
using toofz.TestsShared;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    public sealed class AzureStorageFixture : ICollectionFixture<AzureStorageFixture>, IDisposable
    {
        public AzureStorageFixture()
        {
            if (!AzureStorageEmulatorManager.IsStarted())
            {
                AzureStorageEmulatorManager.Start();
                ShouldStopAzureStorageEmulator = true;
            }
        }

        private bool ShouldStopAzureStorageEmulator;

        public void Dispose()
        {
            if (ShouldStopAzureStorageEmulator)
            {
                AzureStorageEmulatorManager.Stop();
            }
        }
    }
}
