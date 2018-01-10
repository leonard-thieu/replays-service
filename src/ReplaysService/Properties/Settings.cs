using System.Configuration;

namespace toofz.Services.ReplaysService.Properties
{
    [SettingsProvider(typeof(ServiceSettingsProvider))]
    partial class Settings : IReplaysSettings { }
}
