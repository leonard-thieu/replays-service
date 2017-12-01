using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using toofz.NecroDancer.Leaderboards.Steam.Workshop;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    [ExcludeFromCodeCoverage]
    internal sealed class FakeUgcHttpClient : IUgcHttpClient
    {
        public FakeUgcHttpClient()
        {
            var replaysPath = Path.Combine("Data", "SteamWorkshop", "Replays");
            replayFiles = Directory.GetFiles(replaysPath, "*.dat");
        }

        private readonly string[] replayFiles;

        public Task<byte[]> GetUgcFileAsync(
            string url,
            IProgress<long> progress = null,
            CancellationToken cancellationToken = default)
        {
            var uri = new Uri(url);
            var ugcId = long.Parse(uri.Segments[2].TrimEnd('/'));
            var i = (int)(ugcId % replayFiles.Length);

            var ugcFile = File.ReadAllBytes(replayFiles[i]);
            progress?.Report(ugcFile.Length);

            return Task.FromResult(ugcFile);
        }

        public void Dispose() { }
    }
}
