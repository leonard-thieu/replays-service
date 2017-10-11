using System;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
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
