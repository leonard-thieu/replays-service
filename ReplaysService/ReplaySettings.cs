using toofz.NecroDancer.Leaderboards.Services.Common;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    sealed class ReplaySettings : Settings
    {
        public int ReplaysPerUpdate { get; set; }
    }
}
