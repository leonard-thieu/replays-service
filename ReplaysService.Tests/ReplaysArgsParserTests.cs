using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.TestsShared;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    class ReplaysArgsParserTests
    {
        [TestClass]
        public class Parse
        {
            public Parse()
            {
                inReader = mockInReader.Object;
                parser = new ReplaysArgsParser(inReader, outWriter, errorWriter, Constants.Iterations);
            }

            Mock<TextReader> mockInReader = new Mock<TextReader>(MockBehavior.Strict);
            TextReader inReader;
            TextWriter outWriter = new StringWriter();
            TextWriter errorWriter = new StringWriter();
            ReplaysArgsParser parser;

            [TestMethod]
            public void HelpFlagIsSpecified_ShowUsageInformation()
            {
                // Arrange
                string[] args = new[] { "--help" };
                IReplaysSettings settings = Settings.Default;
                settings.Reload();

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.That.NormalizedAreEqual(@"
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
", outWriter.ToString());
            }

            #region ReplaysPerUpdate

            [TestMethod]
            public void ReplaysIsSpecified_SetsReplaysPerUpdate()
            {
                // Arrange
                string[] args = new[] { "--replays=10" };
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual(10, settings.ReplaysPerUpdate);
            }

            #endregion

            #region ToofzApiBaseAddress

            [TestMethod]
            public void ToofzIsSpecified_SetsToofzApiBaseAddress()
            {
                // Arrange
                string[] args = new[] { "--toofz=http://localhost/" };
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual("http://localhost/", settings.ToofzApiBaseAddress);
            }

            #endregion

            #region ToofzApiUserName

            [TestMethod]
            public void UserNameIsSpecified_SetToofzApiUserName()
            {
                // Arrange
                string[] args = new[] { "--username=myUserName" };
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual("myUserName", settings.ToofzApiUserName);
            }

            [TestMethod]
            public void UserNameIsNotSpecifiedAndToofzApiUserNameIsNotSet_PromptsUserForUserNameAndSetsToofzApiUserName()
            {
                // Arrange
                string[] args = new string[0];
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = null,
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myUserName");

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual("myUserName", settings.ToofzApiUserName);
            }

            [TestMethod]
            public void UserNameIsNotSpecifiedAndToofzApiUserNameIsSet_DoesNotSetToofzApiUserName()
            {
                // Arrange
                string[] args = new string[0];
                var mockSettings = new Mock<IReplaysSettings>();
                mockSettings
                    .SetupProperty(s => s.ToofzApiUserName, "myUserName")
                    .SetupProperty(s => s.ToofzApiPassword, new EncryptedSecret("a", Constants.Iterations))
                    .SetupProperty(s => s.SteamWebApiKey, new EncryptedSecret("a", Constants.Iterations));
                var settings = mockSettings.Object;

                // Act
                parser.Parse(args, settings);

                // Assert
                mockSettings.VerifySet(s => s.ToofzApiUserName = It.IsAny<string>(), Times.Never);
            }

            #endregion

            #region ToofzApiPassword

            [TestMethod]
            public void PasswordIsSpecified_SetsToofzApiPassword()
            {
                // Arrange
                string[] args = new[] { "--password=myPassword" };
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myPassword", Constants.Iterations);
                Assert.AreEqual(encrypted.Decrypt(), settings.ToofzApiPassword.Decrypt());
            }

            [TestMethod]
            public void PasswordFlagIsSpecified_PromptsUserForPasswordAndSetsToofzApiPassword()
            {
                // Arrange
                string[] args = new[] { "--password" };
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myPassword");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myPassword", Constants.Iterations);
                Assert.AreEqual(encrypted.Decrypt(), settings.ToofzApiPassword.Decrypt());
            }

            [TestMethod]
            public void PasswordFlagIsNotSpecifiedAndToofzApiPasswordIsNotSet_PromptsUserForPasswordAndSetsToofzApiPassword()
            {
                // Arrange
                string[] args = new string[0];
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = null,
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myPassword");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myPassword", Constants.Iterations);
                Assert.AreEqual(encrypted.Decrypt(), settings.ToofzApiPassword.Decrypt());
            }

            [TestMethod]
            public void PasswordFlagIsNotSpecifiedAndToofzApiPasswordIsSet_DoesNotSetToofzApiPassword()
            {
                // Arrange
                string[] args = new string[0];
                var mockSettings = new Mock<IReplaysSettings>();
                mockSettings
                    .SetupProperty(s => s.ToofzApiUserName, "myUserName")
                    .SetupProperty(s => s.ToofzApiPassword, new EncryptedSecret("a", Constants.Iterations))
                    .SetupProperty(s => s.SteamWebApiKey, new EncryptedSecret("a", Constants.Iterations));
                var settings = mockSettings.Object;

                // Act
                parser.Parse(args, settings);

                // Assert
                mockSettings.VerifySet(s => s.ToofzApiPassword = It.IsAny<EncryptedSecret>(), Times.Never);
            }

            #endregion

            #region SteamWebApiKey

            [TestMethod]
            public void ApikeyIsSpecified_SetsSteamWebApiKey()
            {
                // Arrange
                string[] args = new[] { "--apikey=myApiKey" };
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myApiKey", Constants.Iterations);
                Assert.AreEqual(encrypted.Decrypt(), settings.SteamWebApiKey.Decrypt());
            }

            [TestMethod]
            public void ApikeyFlagIsSpecified_PromptsUserForApikeyAndSetsSteamWebApiKey()
            {
                // Arrange
                string[] args = new[] { "--apikey" };
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myApiKey");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myApiKey", Constants.Iterations);
                Assert.AreEqual(encrypted.Decrypt(), settings.SteamWebApiKey.Decrypt());
            }

            [TestMethod]
            public void ApikeyFlagIsNotSpecifiedAndSteamWebApiKeyIsNotSet_PromptsUserForApikeyAndSetsSteamWebApiKey()
            {
                // Arrange
                string[] args = new string[0];
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = null,
                };
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myApiKey");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myApiKey", Constants.Iterations);
                Assert.AreEqual(encrypted.Decrypt(), settings.SteamWebApiKey.Decrypt());
            }

            [TestMethod]
            public void ApikeyFlagIsNotSpecifiedAndSteamWebApiKeyIsSet_DoesNotSetSteamWebApiKey()
            {
                // Arrange
                string[] args = new string[0];
                var mockSettings = new Mock<IReplaysSettings>();
                mockSettings
                    .SetupProperty(s => s.ToofzApiUserName, "myUserName")
                    .SetupProperty(s => s.ToofzApiPassword, new EncryptedSecret("a", Constants.Iterations))
                    .SetupProperty(s => s.SteamWebApiKey, new EncryptedSecret("a", Constants.Iterations));
                var settings = mockSettings.Object;

                // Act
                parser.Parse(args, settings);

                // Assert
                mockSettings.VerifySet(s => s.SteamWebApiKey = It.IsAny<EncryptedSecret>(), Times.Never);
            }

            #endregion

            #region AzureStorageConnectionString

            [TestMethod]
            public void StorageIsSpecified_SetsAzureStorageConnectionString()
            {
                // Arrange
                string[] args = new[] { "--storage=myConnectionString" };
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myConnectionString", Constants.Iterations);
                Assert.AreEqual(encrypted.Decrypt(), settings.AzureStorageConnectionString.Decrypt());
            }

            [TestMethod]
            public void StorageFlagIsSpecified_PromptsUserForStorageAndSetsAzureStorageConnectionString()
            {
                // Arrange
                string[] args = new[] { "--storage" };
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };
                mockInReader
                    .SetupSequence(r => r.ReadLine())
                    .Returns("myConnectionString");

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret("myConnectionString", Constants.Iterations);
                Assert.AreEqual(encrypted.Decrypt(), settings.AzureStorageConnectionString.Decrypt());
            }

            [TestMethod]
            public void StorageFlagIsNotSpecifiedAndAzureStorageConnectionStringIsNotSet_SetsAzureStorageConnectionStringToDefault()
            {
                // Arrange
                string[] args = new string[0];
                IReplaysSettings settings = new SimpleReplaysSettings
                {
                    ToofzApiUserName = "a",
                    ToofzApiPassword = new EncryptedSecret("a", Constants.Iterations),
                    SteamWebApiKey = new EncryptedSecret("a", Constants.Iterations),
                };

                // Act
                parser.Parse(args, settings);

                // Assert
                var encrypted = new EncryptedSecret(ReplaysArgsParser.DefaultAzureStorageConnectionString, Constants.Iterations);
                Assert.AreEqual(encrypted.Decrypt(), settings.AzureStorageConnectionString.Decrypt());
            }

            [TestMethod]
            public void StorageFlagIsNotSpecifiedAndAzureStorageConnectionStringIsSet_DoesNotSetAzureStorageConnectionString()
            {
                // Arrange
                string[] args = new string[0];
                var mockSettings = new Mock<IReplaysSettings>();
                mockSettings
                    .SetupProperty(s => s.ToofzApiUserName, "myUserName")
                    .SetupProperty(s => s.ToofzApiPassword, new EncryptedSecret("a", Constants.Iterations))
                    .SetupProperty(s => s.SteamWebApiKey, new EncryptedSecret("a", Constants.Iterations))
                    .SetupProperty(s => s.AzureStorageConnectionString, new EncryptedSecret("a", Constants.Iterations));
                var settings = mockSettings.Object;

                // Act
                parser.Parse(args, settings);

                // Assert
                mockSettings.VerifySet(s => s.AzureStorageConnectionString = It.IsAny<EncryptedSecret>(), Times.Never);
            }

            #endregion
        }
    }
}
