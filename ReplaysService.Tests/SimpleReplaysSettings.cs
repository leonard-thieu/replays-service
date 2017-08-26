using System;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    sealed class SimpleReplaysSettings : IReplaysSettings
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

        public void Reload()
        {
            ReplaysPerUpdate = default(int);
            ToofzApiBaseAddress = default(string);
            ToofzApiUserName = default(string);
            ToofzApiPassword = default(EncryptedSecret);
            SteamWebApiKey = default(EncryptedSecret);
            AzureStorageConnectionString = default(EncryptedSecret);
            UpdateInterval = default(TimeSpan);
            DelayBeforeGC = default(TimeSpan);
            InstrumentationKey = default(string);
            KeyDerivationIterations = default(int);
        }

        public void Save() { }
    }
}
