using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal sealed class ReplaysOptions : Options
    {
        /// <summary>
        /// The number of replays to update.
        /// </summary>
        public int? ReplaysPerUpdate { get; set; }
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