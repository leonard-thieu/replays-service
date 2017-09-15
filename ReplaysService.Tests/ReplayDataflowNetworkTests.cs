using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using toofz.NecroDancer.Leaderboards.ReplaysService.Tests.Properties;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamRemoteStorage;
using toofz.NecroDancer.Replays;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    class ReplayDataflowNetworkTests
    {
        [TestClass]
        public class Constructor
        {
            [TestMethod]
            public void SteamWebApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                ISteamWebApiClient steamWebApiClient = null;
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);
                });
            }

            [TestMethod]
            public void AppIdIsNotAPositiveInteger_ThrowsArgumentOutOfRangeException()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 0U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                {
                    new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);
                });
            }

            [TestMethod]
            public void UgcHttpClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                IUgcHttpClient ugcHttpClient = null;
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);
                });
            }

            [TestMethod]
            public void DirectoryIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                ICloudBlobDirectory directory = null;
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);
                });
            }

            [TestMethod]
            public void ReturnsInstance()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;

                // Act
                var network = new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);

                // Assert
                Assert.IsInstanceOfType(network, typeof(ReplayDataflowNetwork));
            }
        }

        [TestClass]
        public class DownloadReplayProperty
        {
            [TestMethod]
            public void ReturnsDownloadReplaySourceBlock()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;
                var network = new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);

                // Act
                var downloadReplay = network.DownloadReplay;

                // Assert
                Assert.IsInstanceOfType(downloadReplay, typeof(ISourceBlock<Replay>));
            }
        }

        [TestClass]
        public class StoreReplayFileProperty
        {
            [TestMethod]
            public void ReturnsStoreReplayFileSourceBlock()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;
                var network = new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);

                // Act
                var storeReplayFile = network.StoreReplayFile;

                // Assert
                Assert.IsInstanceOfType(storeReplayFile, typeof(ISourceBlock<Uri>));
            }
        }

        [TestClass]
        public class PostMethod
        {
            [TestMethod]
            public void AcceptsItem()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;
                var network = new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);
                var ugcId = 849347241492683863;

                // Act
                var accepted = network.Post(ugcId);

                // Assert
                Assert.IsTrue(accepted);
            }
        }

        [TestClass]
        public class CompleteMethod
        {
            [TestMethod]
            public void SignalsCompletion()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;
                var network = new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);
                var ugcId = 849347241492683863;

                // Act
                network.Complete();

                // Assert
                var accepted = network.Post(ugcId);
                Assert.IsFalse(accepted);
            }
        }

        [TestClass]
        public class GetUgcFileDetailsAsyncMethod
        {
            [TestMethod]
            public async Task ReturnsUgcFileDetails()
            {
                // Arrange
                var appId = 247080U;
                var ugcId = 849347241492683863;
                var cancellationToken = CancellationToken.None;
                var mockSteamWebApiClient = new Mock<ISteamWebApiClient>();
                mockSteamWebApiClient
                    .Setup(s => s.GetUgcFileDetailsAsync(appId, ugcId, It.IsAny<IProgress<long>>(), cancellationToken))
                    .Returns(Task.FromResult(new UgcFileDetailsEnvelope
                    {
                        Data = new UgcFileDetails
                        {
                            FileName = "DLC HARDCORE All Chars DLC_PROD_SCORE134377_ZONE11_LEVEL1",
                            Url = "http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/",
                            Size = 999,
                        },
                    }));
                var steamWebApiClient = mockSteamWebApiClient.Object;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var network = new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);

                // Act
                var ugcFileDetails = await network.GetUgcFileDetailsAsync(ugcId);

                // Assert
                Assert.AreEqual("http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/", ugcFileDetails.Data.Url);
            }
        }

        [TestClass]
        public class GetUgcFileAsyncMethod
        {
            [TestMethod]
            public async Task ReturnsUgcFile()
            {
                // Arrange
                var appId = 247080U;
                var cancellationToken = CancellationToken.None;
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var mockUgcHttpClient = new Mock<IUgcHttpClient>();
                mockUgcHttpClient
                    .Setup(c => c.GetUgcFileAsync("http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/", It.IsAny<IProgress<long>>(), cancellationToken))
                    .Returns(Task.FromResult(Resources.RawReplayData));
                var ugcHttpClient = mockUgcHttpClient.Object;
                var directory = Mock.Of<ICloudBlobDirectory>();
                var network = new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);
                var ugcFileDetails = new UgcFileDetailsEnvelope
                {
                    Data = new UgcFileDetails
                    {
                        FileName = "DLC HARDCORE All Chars DLC_PROD_SCORE134377_ZONE11_LEVEL1",
                        Url = "http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/",
                        Size = 999,
                    },
                };

                // Act
                var ugcFile = await network.GetUgcFileAsync(ugcFileDetails);

                // Assert
                Assert.AreEqual(999, ugcFile.Length);
            }
        }

        [TestClass]
        public class ReadReplayDataMethod
        {
            [TestMethod]
            public void ReturnsReplayData()
            {
                // Arrange
                var rawReplayData = Resources.RawReplayData;

                // Act
                var replayData = ReplayDataflowNetwork.ReadReplayData(rawReplayData);

                // Assert
                Assert.AreEqual(94, replayData.Header.Version);
                Assert.AreEqual("LIGHT MINOTAUR", replayData.Header.KilledBy);
            }
        }

        [TestClass]
        public class CreateReplayMethod
        {
            [TestMethod]
            public void ReturnsReplay()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var replayData = new ReplayData
                {
                    Header = new Header
                    {
                        Version = 94,
                        KilledBy = "LIGHT MINOTAUR",
                    },
                };

                // Act
                var replay = ReplayDataflowNetwork.CreateReplay(ugcId, replayData);

                // Assert
                Assert.IsInstanceOfType(replay, typeof(Replay));
            }
        }

        [TestClass]
        public class StoreUgcFileAsyncMethod
        {
            [TestMethod]
            public async Task StoresUgcFile()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var mockBlob = new Mock<ICloudBlockBlob>();
                mockBlob
                    .SetupGet(b => b.Properties)
                    .Returns(new BlobProperties());
                var blob = mockBlob.Object;
                var mockDirectory = new Mock<ICloudBlobDirectory>();
                mockDirectory
                    .Setup(d => d.GetBlockBlobReference(It.IsAny<string>()))
                    .Returns(blob);
                var directory = mockDirectory.Object;
                var cancellationToken = CancellationToken.None;
                var network = new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);
                var replayData = new ReplayData
                {
                    Header = new Header(),
                };
                var replay = new Replay();

                // Act
                await network.StoreUgcFileAsync(replayData, replay);

                // Assert
                mockBlob.Verify(b => b.UploadFromStreamAsync(It.IsAny<Stream>(), cancellationToken), Times.Once);
            }
        }
    }
}
