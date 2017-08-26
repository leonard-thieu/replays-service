using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    public interface IReplaysSettings : ISettings
    {
        uint AppId { get; }
        int ReplaysPerUpdate { get; set; }
        string ToofzApiBaseAddress { get; set; }
        string ToofzApiUserName { get; set; }
        EncryptedSecret ToofzApiPassword { get; set; }
        EncryptedSecret SteamWebApiKey { get; set; }
        EncryptedSecret AzureStorageConnectionString { get; set; }
    }
}