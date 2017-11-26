using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamRemoteStorage;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamUser;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal sealed class FakeSteamWebApiClient : ISteamWebApiClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FakeSteamWebApiClient));

        public FakeSteamWebApiClient()
        {
            Log.Warn("Using test data for calls to Steam Web API. Set your Steam Web API key to use the actual Steam Web API.");
            Log.Warn("Run this application with --help to find out how to set your Steam Web API key.");

            var ugcFileDetailsPath = Path.Combine("Data", "SteamWebApi", "UgcFileDetails");
            ugcFileDetailsFiles = Directory.GetFiles(ugcFileDetailsPath, "*.json");
        }

        private readonly string[] ugcFileDetailsFiles;

        public string SteamWebApiKey { get; set; }

        public Task<PlayerSummariesEnvelope> GetPlayerSummariesAsync(
            IEnumerable<long> steamIds,
            IProgress<long> progress = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UgcFileDetailsEnvelope> GetUgcFileDetailsAsync(
            uint appId,
            long ugcId,
            IProgress<long> progress = null,
            CancellationToken cancellationToken = default)
        {
            var i = (int)(ugcId % ugcFileDetailsFiles.Length);
            using (var sr = File.OpenText(ugcFileDetailsFiles[i]))
            {
                var ugcFileDetails = JsonConvert.DeserializeObject<UgcFileDetailsEnvelope>(sr.ReadToEnd());
                progress?.Report(sr.BaseStream.Length);

                return Task.FromResult(ugcFileDetails);
            }
        }

        public void Dispose() { }
    }
}
