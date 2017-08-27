using System.Configuration;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Properties
{
    [SettingsProvider(typeof(ServiceSettingsProvider))]
    partial class Settings : IReplaysSettings { }
}
