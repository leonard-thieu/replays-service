using System;
using System.IO;
using Moq;
using toofz.Services.ReplaysService.Properties;
using Xunit;

namespace toofz.Services.ReplaysService.Tests
{
    public class ReplaysArgsParserTests
    {
        public ReplaysArgsParserTests()
        {
            inReader = mockInReader.Object;
            parser = new ReplaysArgsParser(inReader, outWriter, errorWriter);
        }

        private Mock<TextReader> mockInReader = new Mock<TextReader>(MockBehavior.Strict);
        private TextReader inReader;
        private TextWriter outWriter = new StringWriter();
        private TextWriter errorWriter = new StringWriter();
        private ReplaysArgsParser parser;

        public class ParseMethod : ReplaysArgsParserTests
        {
            public ParseMethod()
            {
                settings.SteamWebApiKey = new EncryptedSecret("steamWebApiKey", 1);
                settings.AzureStorageConnectionString = new EncryptedSecret("connectionString", 1);
                settings.KeyDerivationIterations = 1;
                settings.ReplaysPerUpdate = 1;
            }

            private readonly IReplaysSettings settings = new StubReplaysSettings();

            [DisplayFact]
            public void HelpFlagIsSpecified_ShowUsageInformation()
            {
                // Arrange
                string[] args = { "--help" };
                IReplaysSettings settings = Settings.Default;
                settings.Reload();

                // Act
                parser.Parse(args, settings);

                // Assert
                var output = outWriter.ToString();
                Assert.Equal(@"
Usage: ReplaysService.exe [options]

options:
  --help                Shows usage information.
  --interval=VALUE      The minimum amount of time that should pass between each cycle.
  --delay=VALUE         The amount of time to wait after a cycle to perform garbage collection.
  --ikey=VALUE          An Application Insights instrumentation key.
  --iterations=VALUE    The number of rounds to execute a key derivation function.
  --connection[=VALUE]  The connection string used to connect to the leaderboards database.
  --replays=VALUE       The number of replays to update.
  --apikey[=VALUE]      A Steam Web API key.
  --storage[=VALUE]     An Azure Storage connection string.
", output, ignoreLineEndingDifferences: true);
            }

            #region ReplaysPerUpdate

            [DisplayFact(nameof(IReplaysSettings.ReplaysPerUpdate))]
            public void ReplaysIsSpecified_SetsReplaysPerUpdate()
            {
                // Arrange
                string[] args = { "--replays=10" };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.Equal(10, settings.ReplaysPerUpdate);
            }

            #endregion

            #region SteamWebApiKey

            [DisplayFact(nameof(IReplaysSettings.SteamWebApiKey))]
            public void ApikeyIsSpecified_SetsSteamWebApiKey()
            {
                // Arrange
                string[] args = { "--apikey=myApiKey" };

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myApiKey", 1);
                Assert.Equal(encrypted.Decrypt(), settings.SteamWebApiKey.Decrypt());
            }

            [DisplayFact(nameof(IReplaysSettings.SteamWebApiKey))]
            public void ApikeyFlagIsSpecified_PromptsUserForApikeyAndSetsSteamWebApiKey()
            {
                // Arrange
                string[] args = { "--apikey" };
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myApiKey");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myApiKey", 1);
                Assert.Equal(encrypted.Decrypt(), settings.SteamWebApiKey.Decrypt());
            }

            [DisplayFact(nameof(IReplaysSettings.SteamWebApiKey))]
            public void ApikeyFlagIsNotSpecifiedAndSteamWebApiKeyIsSet_DoesNotSetSteamWebApiKey()
            {
                // Arrange
                string[] args = { };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.Equal("steamWebApiKey", settings.SteamWebApiKey.Decrypt());
            }

            #endregion

            #region AzureStorageConnectionString

            [DisplayFact(nameof(IReplaysSettings.AzureStorageConnectionString))]
            public void StorageIsSpecified_SetsAzureStorageConnectionString()
            {
                // Arrange
                string[] args = { "--storage=myConnectionString" };

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myConnectionString", 1);
                Assert.Equal(encrypted.Decrypt(), settings.AzureStorageConnectionString.Decrypt());
            }

            [DisplayFact(nameof(IReplaysSettings.AzureStorageConnectionString))]
            public void StorageFlagIsSpecified_PromptsUserForStorageAndSetsAzureStorageConnectionString()
            {
                // Arrange
                string[] args = { "--storage" };
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myConnectionString");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myConnectionString", 1);
                Assert.Equal(encrypted.Decrypt(), settings.AzureStorageConnectionString.Decrypt());
            }

            [DisplayFact(nameof(IReplaysSettings.AzureStorageConnectionString))]
            public void StorageFlagIsNotSpecifiedAndAzureStorageConnectionStringIsNotSet_SetsAzureStorageConnectionStringToDefault()
            {
                // Arrange
                string[] args = { };
                settings.AzureStorageConnectionString = null;

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret(ReplaysArgsParser.DefaultAzureStorageConnectionString, 1);
                Assert.Equal(encrypted.Decrypt(), settings.AzureStorageConnectionString.Decrypt());
            }

            [DisplayFact(nameof(IReplaysSettings.AzureStorageConnectionString))]
            public void StorageFlagIsNotSpecifiedAndAzureStorageConnectionStringIsSet_DoesNotSetAzureStorageConnectionString()
            {
                // Arrange
                string[] args = { };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.Equal("connectionString", settings.AzureStorageConnectionString.Decrypt());
            }

            #endregion

            private class StubReplaysSettings : IReplaysSettings
            {
                public uint AppId => 247080;

                public int ReplaysPerUpdate { get; set; }
                public EncryptedSecret SteamWebApiKey { get; set; }
                public EncryptedSecret AzureStorageConnectionString { get; set; }
                public TimeSpan UpdateInterval { get; set; }
                public TimeSpan DelayBeforeGC { get; set; }
                public string InstrumentationKey { get; set; }
                public int KeyDerivationIterations { get; set; }
                public EncryptedSecret LeaderboardsConnectionString { get; set; }

                public void Reload() { }

                public void Save() { }
            }
        }
    }
}
