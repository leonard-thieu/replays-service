using System.Configuration;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Properties
{
    [SettingsProvider(typeof(ServiceSettingsProvider))]
    partial class Settings : IReplaysSettings { }
}
