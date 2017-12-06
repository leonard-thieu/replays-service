using System;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using toofz.Data;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    [ExcludeFromCodeCoverage]
    internal sealed class FakeLeaderboardsContext : ILeaderboardsContext
    {
        public FakeLeaderboardsContext()
        {
            var ugcFileDetailsPath = Path.Combine("Data", "SteamWebApi", "UgcFileDetails");
            var ugcFileDetailsFiles = Directory.GetFiles(ugcFileDetailsPath, "*.json");
            var replays = (from f in ugcFileDetailsFiles
                           let n = Path.GetFileNameWithoutExtension(f)
                           select new Replay
                           {
                               ReplayId = long.Parse(n),
                           })
                           .ToList();
            Replays = new FakeDbSet<Replay>(replays);
        }

        public DbSet<Replay> Replays { get; }

        public DbSet<Leaderboard> Leaderboards => throw new NotImplementedException();
        public DbSet<Entry> Entries => throw new NotImplementedException();
        public DbSet<DailyLeaderboard> DailyLeaderboards => throw new NotImplementedException();
        public DbSet<DailyEntry> DailyEntries => throw new NotImplementedException();
        public DbSet<Player> Players => throw new NotImplementedException();
        public DbSet<Product> Products => throw new NotImplementedException();
        public DbSet<Mode> Modes => throw new NotImplementedException();
        public DbSet<Run> Runs => throw new NotImplementedException();
        public DbSet<Character> Characters => throw new NotImplementedException();

        public void Dispose() { }
    }
}
