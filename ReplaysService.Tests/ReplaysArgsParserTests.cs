using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
                parser = new ReplaysArgsParser(inReader, outWriter, errorWriter);
            }

            Mock<TextReader> mockInReader = new Mock<TextReader>(MockBehavior.Strict);
            TextReader inReader;
            TextWriter outWriter = new StringWriter();
            TextWriter errorWriter = new StringWriter();
            ReplaysArgsParser parser;

            [TestMethod]
            public void ReplaysIsSpecified_SetsReplaysPerUpdateToReplays()
            {
                // Arrange
                string[] args = new[] { "--replays=10" };
                IReplaysSettings settings = new SimpleReplaysSettings();

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual(10, settings.ReplaysPerUpdate);
            }

            [TestMethod]
            public void ToofzIsSpecified_SetsToofzApiBaseAddressToToofz()
            {
                // Arrange
                string[] args = new[] { "--toofz=http://localhost/" };
                IReplaysSettings settings = new SimpleReplaysSettings();

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual("http://localhost/", settings.ToofzApiBaseAddress);
            }

            [TestMethod]
            public void UserNameIsSpecified_SetToofzApiUserNameToUserName()
            {
                // Arrange
                string[] args = new[] { "--username=myUser" };
                IReplaysSettings settings = new SimpleReplaysSettings();

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual("myUser", settings.ToofzApiUserName);
            }

            [TestMethod]
            public void PasswordIsSpecified_SetsToofzApiPasswordToEncryptedPassword()
            {
                // Arrange
                string[] args = new[] { "--password=myPassword" };
                IReplaysSettings settings = new SimpleReplaysSettings();
                var encrypted = new EncryptedSecret("myPassword");

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual(encrypted.Decrypt(), settings.ToofzApiPassword.Decrypt());
            }

            [TestMethod]
            public void ApikeyIsSpecified_SetsSteamWebApiKeyToEncryptedApikey()
            {
                // Arrange
                string[] args = new[] { "--apikey=myApiKey" };
                IReplaysSettings settings = new SimpleReplaysSettings();
                var encrypted = new EncryptedSecret("myApiKey");

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual(encrypted.Decrypt(), settings.SteamWebApiKey.Decrypt());
            }

            [TestMethod]
            public void ConnectionIsNotSpecifiedAndLeaderboardsConnectionStringIsNull_SetsLeaderboardsConnectionStringToDefault()
            {
                // Arrange
                string[] args = new string[0];
                var mockSettings = new Mock<IReplaysSettings>();
                mockSettings
                    .SetupProperty(s => s.ToofzApiBaseAddress, "http://localhost/")
                    .SetupProperty(s => s.ToofzApiUserName, "myUserName")
                    .SetupProperty(s => s.ToofzApiPassword, new EncryptedSecret("myPassword"))
                    .SetupProperty(s => s.SteamWebApiKey, new EncryptedSecret("myApiKey"))
                    .SetupProperty(s => s.AzureStorageConnectionString);
                var settings = mockSettings.Object;
                var encrypted = new EncryptedSecret(ReplaysArgsParser.DefaultAzureStorageConnectionString);

                // Act
                parser.Parse(args, settings);

                // Assert
                Assert.AreEqual(encrypted.Decrypt(), settings.AzureStorageConnectionString.Decrypt());
            }
        }
    }
}
