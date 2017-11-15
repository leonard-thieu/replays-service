using System;
using System.IO;
using Moq;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.Services;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
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
                settings.ToofzApiUserName = "userName";
                settings.ToofzApiPassword = new EncryptedSecret("password", 1);
                settings.SteamWebApiKey = new EncryptedSecret("steamWebApiKey", 1);
                settings.AzureStorageConnectionString = new EncryptedSecret("connectionString", 1);
                settings.KeyDerivationIterations = 1;
                settings.ReplaysPerUpdate = 1;
            }

            private readonly IReplaysSettings settings = new StubReplaysSettings();

            [Fact]
            public void HelpFlagIsSpecified_ShowUsageInformation()
            {
                // Arrange
                string[] args = { "--help" };
                IReplaysSettings settings = Settings.Default;
                settings.Reload();

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.Equal(@"
Usage: ReplaysService.exe [options]

options:
  --help              Shows usage information.
  --interval=VALUE    The minimum amount of time that should pass between each cycle.
  --delay=VALUE       The amount of time to wait after a cycle to perform garbage collection.
  --ikey=VALUE        An Application Insights instrumentation key.
  --iterations=VALUE  The number of rounds to execute a key derivation function.
  --replays=VALUE     The number of replays to update.
  --toofz=VALUE       The base address of toofz API.
  --username=VALUE    The user name used to log on to toofz API.
  --password[=VALUE]  The password used to log on to toofz API.
  --apikey[=VALUE]    A Steam Web API key.
  --storage[=VALUE]   An Azure Storage connection string.
", outWriter.ToString(), ignoreLineEndingDifferences: true);
            }

            #region ReplaysPerUpdate

            [Fact]
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

            #region ToofzApiBaseAddress

            [Fact]
            public void ToofzIsSpecified_SetsToofzApiBaseAddress()
            {
                // Arrange
                string[] args = { "--toofz=http://localhost/" };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.Equal("http://localhost/", settings.ToofzApiBaseAddress);
            }

            #endregion

            #region ToofzApiUserName

            [Fact]
            public void UserNameIsSpecified_SetToofzApiUserName()
            {
                // Arrange
                string[] args = { "--username=myUserName" };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.Equal("myUserName", settings.ToofzApiUserName);
            }

            [Fact]
            public void UserNameIsNotSpecifiedAndToofzApiUserNameIsNotSet_PromptsUserForUserNameAndSetsToofzApiUserName()
            {
                // Arrange
                string[] args = { };
                settings.ToofzApiUserName = null;
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myUserName");

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.Equal("myUserName", settings.ToofzApiUserName);
            }

            [Fact]
            public void UserNameIsNotSpecifiedAndToofzApiUserNameIsSet_DoesNotSetToofzApiUserName()
            {
                // Arrange
                string[] args = { };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.Equal("userName", settings.ToofzApiUserName);
            }

            #endregion

            #region ToofzApiPassword

            [Fact]
            public void PasswordIsSpecified_SetsToofzApiPassword()
            {
                // Arrange
                string[] args = { "--password=myPassword" };

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myPassword", 1);
                Assert.Equal(encrypted.Decrypt(), settings.ToofzApiPassword.Decrypt());
            }

            [Fact]
            public void PasswordFlagIsSpecified_PromptsUserForPasswordAndSetsToofzApiPassword()
            {
                // Arrange
                string[] args = { "--password" };
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myPassword");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myPassword", 1);
                Assert.Equal(encrypted.Decrypt(), settings.ToofzApiPassword.Decrypt());
            }

            [Fact]
            public void PasswordFlagIsNotSpecifiedAndToofzApiPasswordIsNotSet_PromptsUserForPasswordAndSetsToofzApiPassword()
            {
                // Arrange
                string[] args = { };
                settings.ToofzApiPassword = null;
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myPassword");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myPassword", 1);
                Assert.Equal(encrypted.Decrypt(), settings.ToofzApiPassword.Decrypt());
            }

            [Fact]
            public void PasswordFlagIsNotSpecifiedAndToofzApiPasswordIsSet_DoesNotSetToofzApiPassword()
            {
                // Arrange
                string[] args = { };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.Equal("password", settings.ToofzApiPassword.Decrypt());
            }

            #endregion

            #region SteamWebApiKey

            [Fact]
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

            [Fact]
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

            [Fact]
            public void ApikeyFlagIsNotSpecifiedAndSteamWebApiKeyIsNotSet_PromptsUserForApikeyAndSetsSteamWebApiKey()
            {
                // Arrange
                string[] args = { };
                settings.SteamWebApiKey = null;
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myApiKey");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myApiKey", 1);
                Assert.Equal(encrypted.Decrypt(), settings.SteamWebApiKey.Decrypt());
            }

            [Fact]
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

            [Fact]
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

            [Fact]
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

            [Fact]
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

            [Fact]
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
                public string ToofzApiBaseAddress { get; set; }
                public string ToofzApiUserName { get; set; }
                public EncryptedSecret ToofzApiPassword { get; set; }
                public EncryptedSecret SteamWebApiKey { get; set; }
                public EncryptedSecret AzureStorageConnectionString { get; set; }
                public TimeSpan UpdateInterval { get; set; }
                public TimeSpan DelayBeforeGC { get; set; }
                public string InstrumentationKey { get; set; }
                public int KeyDerivationIterations { get; set; }

                public void Reload() { }

                public void Save() { }
            }
        }
    }
}
