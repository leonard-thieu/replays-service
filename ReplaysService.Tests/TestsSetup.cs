using Microsoft.VisualStudio.TestTools.UnitTesting;
using toofz.TestsShared;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    static class TestsSetup
    {
        public static bool ShouldStopAzureStorageEmulator = false;

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            if (ShouldStopAzureStorageEmulator)
            {
                AzureStorageEmulatorManager.Stop();
            }
        }
    }
}
