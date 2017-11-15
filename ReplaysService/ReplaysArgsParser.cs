using System;
using System.IO;
using System.Reflection;
using Mono.Options;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal sealed class ReplaysArgsParser : ArgsParser<ReplaysOptions, IReplaysSettings>
    {
        internal const string DefaultAzureStorageConnectionString = "UseDevelopmentStorage=true";

        public ReplaysArgsParser(TextReader inReader, TextWriter outWriter, TextWriter errorWriter) : base(inReader, outWriter, errorWriter) { }

        protected override string EntryAssemblyFileName { get; } = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

        protected override void OnParsing(Type settingsType, OptionSet optionSet, ReplaysOptions options)
        {
            base.OnParsing(settingsType, optionSet, options);

            optionSet.Add("replays=", GetDescription(settingsType, nameof(Settings.ReplaysPerUpdate)), (int replays) => options.ReplaysPerUpdate = replays);
            optionSet.Add("toofz=", GetDescription(settingsType, nameof(Settings.ToofzApiBaseAddress)), api => options.ToofzApiBaseAddress = api);
            optionSet.Add("username=", GetDescription(settingsType, nameof(Settings.ToofzApiUserName)), username => options.ToofzApiUserName = username);
            optionSet.Add("password:", GetDescription(settingsType, nameof(Settings.ToofzApiPassword)), password => options.ToofzApiPassword = password);
            optionSet.Add("apikey:", GetDescription(settingsType, nameof(Settings.SteamWebApiKey)), apikey => options.SteamWebApiKey = apikey);
            optionSet.Add("storage:", GetDescription(settingsType, nameof(Settings.AzureStorageConnectionString)), storage => options.AzureStorageConnectionString = storage);
        }

        protected override void OnParsed(ReplaysOptions options, IReplaysSettings settings)
        {
            base.OnParsed(options, settings);

            var iterations = settings.KeyDerivationIterations;

            #region ReplaysPerUpdate

            var replaysPerUpdate = options.ReplaysPerUpdate;
            if (replaysPerUpdate != null)
            {
                settings.ReplaysPerUpdate = replaysPerUpdate.Value;
            }

            #endregion

            #region ToofzApiBaseAddress

            var toofzApiBaseAddress = options.ToofzApiBaseAddress;
            if (!string.IsNullOrEmpty(toofzApiBaseAddress))
            {
                settings.ToofzApiBaseAddress = toofzApiBaseAddress;
            }

            #endregion

            #region ToofzApiUserName

            var toofzApiUserName = options.ToofzApiUserName;
            if (!string.IsNullOrEmpty(toofzApiUserName))
            {
                settings.ToofzApiUserName = toofzApiUserName;
            }
            else if (string.IsNullOrEmpty(settings.ToofzApiUserName))
            {
                settings.ToofzApiUserName = ReadOption("toofz API user name");
            }

            #endregion

            #region ToofzApiPassword

            var toofzApiPassword = options.ToofzApiPassword;
            if (ShouldPromptForRequiredSetting(toofzApiPassword, settings.ToofzApiPassword))
            {
                toofzApiPassword = ReadOption("toofz API password");
            }

            if (toofzApiPassword != "")
            {
                settings.ToofzApiPassword = new EncryptedSecret(toofzApiPassword, iterations);
            }

            #endregion

            #region SteamWebApiKey

            var steamWebApiKey = options.SteamWebApiKey;
            if (ShouldPromptForRequiredSetting(steamWebApiKey, settings.SteamWebApiKey))
            {
                steamWebApiKey = ReadOption("Steam Web API key");
            }

            if (steamWebApiKey != "")
            {
                settings.SteamWebApiKey = new EncryptedSecret(steamWebApiKey, iterations);
            }

            #endregion

            #region AzureStorageConnectionString

            var azureStorageConnectionString = options.AzureStorageConnectionString;
            if (!string.IsNullOrEmpty(azureStorageConnectionString))
            {
                settings.AzureStorageConnectionString = new EncryptedSecret(azureStorageConnectionString, iterations);
            }
            else if (azureStorageConnectionString == null)
            {
                azureStorageConnectionString = ReadOption("Azure Storage connection string");
                settings.AzureStorageConnectionString = new EncryptedSecret(azureStorageConnectionString, iterations);
            }

            if (settings.AzureStorageConnectionString == null)
            {
                settings.AzureStorageConnectionString = new EncryptedSecret(DefaultAzureStorageConnectionString, iterations);
            }

            #endregion
        }
    }
}