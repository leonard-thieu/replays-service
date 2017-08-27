using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Properties
{
    public interface IReplaysSettings : ISettings
    {
        /// <summary>
        /// The product's application ID.
        /// </summary>
        uint AppId { get; }
        /// <summary>
        /// The number of replays to update.
        /// </summary>
        int ReplaysPerUpdate { get; set; }
        /// <summary>
        /// The base address of toofz API.
        /// </summary>
        string ToofzApiBaseAddress { get; set; }
        /// <summary>
        /// The user name used to log on to toofz API.
        /// </summary>
        string ToofzApiUserName { get; set; }
        /// <summary>
        /// The password used to log on to toofz API.
        /// </summary>
        EncryptedSecret ToofzApiPassword { get; set; }
        /// <summary>
        /// A Steam Web API key.
        /// </summary>
        EncryptedSecret SteamWebApiKey { get; set; }
        /// <summary>
        /// An Azure Storage connection string.
        /// </summary>
        EncryptedSecret AzureStorageConnectionString { get; set; }
    }
}