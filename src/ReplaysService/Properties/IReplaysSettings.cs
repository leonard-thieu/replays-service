namespace toofz.Services.ReplaysService.Properties
{
    internal interface IReplaysSettings : ISettings
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
        /// A Steam Web API key.
        /// </summary>
        EncryptedSecret SteamWebApiKey { get; set; }
        /// <summary>
        /// An Azure Storage connection string.
        /// </summary>
        EncryptedSecret AzureStorageConnectionString { get; set; }
    }
}