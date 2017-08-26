using System;
using System.IO;
using System.Reflection;
using Mono.Options;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.Services;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    sealed class ReplaysArgsParser : ArgsParser<ReplaysOptions, IReplaysSettings>
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
            optionSet.Add("storage", GetDescription(settingsType, nameof(Settings.AzureStorageConnectionString)), storage => options.AzureStorageConnectionString = storage);
        }

        protected override void OnParsed(ReplaysOptions options, IReplaysSettings settings)
        {
            base.OnParsed(options, settings);

            #region ReplaysPerUpdate

            if (options.ReplaysPerUpdate != null)
            {
                settings.ReplaysPerUpdate = options.ReplaysPerUpdate.Value;
            }

            #endregion

            #region ToofzApiBaseAddress

            if (!string.IsNullOrEmpty(options.ToofzApiBaseAddress))
            {
                settings.ToofzApiBaseAddress = options.ToofzApiBaseAddress;
            }

            #endregion

            #region ToofzApiUserName

            if (!string.IsNullOrEmpty(options.ToofzApiUserName))
            {
                settings.ToofzApiUserName = options.ToofzApiUserName;
            }

            while (string.IsNullOrEmpty(settings.ToofzApiUserName))
            {
                OutWriter.Write("toofz API user name: ");
                settings.ToofzApiUserName = InReader.ReadLine();
            }

            #endregion

            #region ToofzApiPassword

            if (!string.IsNullOrEmpty(options.ToofzApiPassword))
            {
                settings.ToofzApiPassword = new EncryptedSecret(options.ToofzApiPassword);
            }

            // When options.ToofzApiPassword == null, the user has indicated that they wish to be prompted to enter the password.
            while (settings.ToofzApiPassword == null || options.ToofzApiPassword == null)
            {
                OutWriter.Write("toofz API password: ");
                options.ToofzApiPassword = InReader.ReadLine();
                if (!string.IsNullOrEmpty(options.ToofzApiPassword))
                {
                    settings.ToofzApiPassword = new EncryptedSecret(options.ToofzApiPassword);
                }
            }

            #endregion

            #region SteamWebApiKey

            if (!string.IsNullOrEmpty(options.SteamWebApiKey))
            {
                settings.SteamWebApiKey = new EncryptedSecret(options.SteamWebApiKey);
            }

            // When options.SteamWebApiKey == null, the user has indicated that they wish to be prompted to enter the password.
            while (settings.SteamWebApiKey == null || options.SteamWebApiKey == null)
            {
                OutWriter.Write("Steam Web API key: ");
                options.SteamWebApiKey = InReader.ReadLine();
                if (!string.IsNullOrEmpty(options.SteamWebApiKey))
                {
                    settings.SteamWebApiKey = new EncryptedSecret(options.SteamWebApiKey);
                }
            }

            #endregion

            #region AzureStorageConnectionString

            if (!string.IsNullOrEmpty(options.AzureStorageConnectionString))
            {
                settings.AzureStorageConnectionString = new EncryptedSecret(options.AzureStorageConnectionString);
            }
            else
            {
                if (options.AzureStorageConnectionString == "" && settings.AzureStorageConnectionString == null)
                {
                    settings.AzureStorageConnectionString = new EncryptedSecret(DefaultAzureStorageConnectionString);
                }
                else
                {
                    // When options.AzureStorageConnectionString == null, the user has indicated that they wish to be prompted to enter the connection string.
                    while (options.AzureStorageConnectionString == null)
                    {
                        OutWriter.Write("Azure Storage connection string: ");
                        options.AzureStorageConnectionString = InReader.ReadLine();
                        if (!string.IsNullOrEmpty(options.AzureStorageConnectionString))
                        {
                            settings.AzureStorageConnectionString = new EncryptedSecret(options.AzureStorageConnectionString);
                        }
                    }
                }
            }

            #endregion
        }
    }
}