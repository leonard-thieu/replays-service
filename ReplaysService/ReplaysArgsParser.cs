using System;
using System.IO;
using System.Reflection;
using Microsoft.WindowsAzure.Storage;
using Mono.Options;
using toofz.Services.ReplaysService.Properties;

namespace toofz.Services.ReplaysService
{
    internal sealed class ReplaysArgsParser : ArgsParser<ReplaysOptions, IReplaysSettings>
    {
        internal static readonly string DefaultAzureStorageConnectionString = CloudStorageAccount.DevelopmentStorageAccount.ToString(exportSecrets: true);

        public ReplaysArgsParser(TextReader inReader, TextWriter outWriter, TextWriter errorWriter) : base(inReader, outWriter, errorWriter) { }

        protected override string EntryAssemblyFileName { get; } = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

        protected override void OnParsing(Type settingsType, OptionSet optionSet, ReplaysOptions options)
        {
            base.OnParsing(settingsType, optionSet, options);

            optionSet.Add("replays=", GetDescription(settingsType, nameof(Settings.ReplaysPerUpdate)), (int replays) => options.ReplaysPerUpdate = replays);
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

            #region SteamWebApiKey

            var steamWebApiKey = options.SteamWebApiKey;
            if (ShouldPrompt(steamWebApiKey))
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
            if (azureStorageConnectionString == null)
            {
                azureStorageConnectionString = ReadOption("Azure Storage connection string");
            }

            if (azureStorageConnectionString != "")
            {
                settings.AzureStorageConnectionString = new EncryptedSecret(azureStorageConnectionString, iterations);
            }
            else if (settings.AzureStorageConnectionString == null)
            {
                settings.AzureStorageConnectionString = new EncryptedSecret(DefaultAzureStorageConnectionString, iterations);
            }

            #endregion
        }
    }
}