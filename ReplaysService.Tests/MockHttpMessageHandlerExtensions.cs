using RichardSzalay.MockHttp;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    internal static class MockHttpMessageHandlerExtensions
    {
        public static MockedRequest RespondWithUgcFileDetails(this MockHttpMessageHandler handler, long ugcId, string content)
        {
            return handler
                .When("https://api.steampowered.com/ISteamRemoteStorage/GetUGCFileDetails/v1")
                .WithQueryString("key", "mySteamWebApiKey")
                .WithQueryString("appid", 247080.ToString())
                .WithQueryString("ugcid", ugcId.ToString())
                .RespondWithJson(content);
        }
    }
}
