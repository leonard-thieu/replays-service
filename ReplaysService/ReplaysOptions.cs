using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    sealed class ReplaysOptions : Options
    {
        public int? ReplaysPerUpdate { get; internal set; }
        public string ToofzApiBaseAddress { get; internal set; }
        public string ToofzApiUserName { get; internal set; }
        public string ToofzApiPassword { get; internal set; } = "";
        public string SteamWebApiKey { get; internal set; } = "";
        public string AzureStorageConnectionString { get; internal set; } = "";
    }
}