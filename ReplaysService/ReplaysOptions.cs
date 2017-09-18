using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    sealed class ReplaysOptions : Options
    {
        /// <summary>
        /// The number of replays to update.
        /// </summary>
        public int? ReplaysPerUpdate { get; set; }
        /// <summary>
        /// The base address of toofz API.
        /// </summary>
        public string ToofzApiBaseAddress { get; set; }
        /// <summary>
        /// The user name used to log on to toofz API.
        /// </summary>
        public string ToofzApiUserName { get; set; }
        /// <summary>
        /// The password used to log on to toofz API.
        /// </summary>
        public string ToofzApiPassword { get; set; } = "";
        /// <summary>
        /// A Steam Web API key.
        /// </summary>
        public string SteamWebApiKey { get; set; } = "";
        /// <summary>
        /// An Azure Storage connection string.
        /// </summary>
        public string AzureStorageConnectionString { get; set; } = "";
    }
}