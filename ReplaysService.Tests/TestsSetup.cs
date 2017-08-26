using Microsoft.VisualStudio.TestTools.UnitTesting;
using toofz.TestsShared;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    [TestClass]
    public class TestsSetup
    {
        static bool shouldStop;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            if (!AzureStorageEmulatorManager.IsProcessStarted())
            {
                AzureStorageEmulatorManager.StartStorageEmulator();
                shouldStop = true;
            }
            else
            {
                shouldStop = false;
            }

        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            if (shouldStop)
            {
                AzureStorageEmulatorManager.StopStorageEmulator();
            }
        }
    }
}
