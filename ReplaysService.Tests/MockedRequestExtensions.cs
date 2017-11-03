using System.Net.Http;
using System.Text;
using RichardSzalay.MockHttp;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    internal static class MockedRequestExtensions
    {
        public static MockedRequest RespondWithJson(this MockedRequest handler, string content)
        {
            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            return handler.Respond(httpContent);
        }
    }
}
