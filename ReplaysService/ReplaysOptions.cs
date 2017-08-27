using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    sealed class ReplaysOptions : Options
    {
        /// <summary>
        /// The number of replays to update.
        /// </summary>
        public int? ReplaysPerUpdate { get; internal set; }
        /// <summary>
        /// The base address of toofz API.
        /// </summary>
        public string ToofzApiBaseAddress { get; internal set; }
        /// <summary>
        /// The user name used to log on to toofz API.
        /// </summary>
        public string ToofzApiUserName { get; internal set; }
        /// <summary>
        /// The password used to log on to toofz API.
        /// </summary>
        public string ToofzApiPassword { get; internal set; } = "";
        /// <summary>
        /// A Steam Web API key.
        /// </summary>
        public string SteamWebApiKey { get; internal set; } = "";
        /// <summary>
        /// An Azure Storage connection string.
        /// </summary>
        public string AzureStorageConnectionString { get; internal set; } = "";
    }
}