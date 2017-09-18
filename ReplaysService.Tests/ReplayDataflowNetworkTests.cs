using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using RichardSzalay.MockHttp;
using toofz.NecroDancer.Leaderboards.ReplaysService.Tests.Properties;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamRemoteStorage;
using toofz.NecroDancer.Replays;
using toofz.TestsShared;

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
        public class SendAsyncMethod
        {
            [TestMethod]
            public async Task AcceptsItem()
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
                var accepted = await network.SendAsync(ugcId, cancellationToken);

                // Assert
                Assert.IsTrue(accepted);
            }
        }

        [TestClass]
        public class CompleteMethod
        {
            [TestMethod]
            public async Task SignalsCompletion()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;
                var network = new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);

                // Act
                network.Complete();

                // Assert
                var completion = Task.WhenAll(network.DownloadReplay.Completion, network.StoreReplayFile.Completion);
                await completion;
                Assert.IsTrue(completion.IsCompleted);
            }
        }

        [TestClass]
        public class GetUgcFileDetailsAsyncMethod
        {
            [TestMethod]
            public async Task GetUgcFileDetailsAsyncThrowsHttpRequestStatusException_SetsUgcFileDetailsException()
            {
                // Arrange
                var appId = 247080U;
                var ugcId = 849347241492683863;
                var cancellationToken = CancellationToken.None;
                var steamWebApiClientHandler = new MockHttpMessageHandler();
                steamWebApiClientHandler.When("*").Respond(HttpStatusCode.BadRequest);
                var steamWebApiClientHandlers = HttpClientFactory.CreatePipeline(steamWebApiClientHandler, new DelegatingHandler[]
                {
                    new SteamWebApiTransientFaultHandler(),
                });
                var steamWebApiClient = new SteamWebApiClient(steamWebApiClientHandlers) { SteamWebApiKey = "mySteamWebApiKey" };
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var network = new ReplayDataflowNetwork(steamWebApiClient, appId, ugcHttpClient, directory, cancellationToken);
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(ugcId);

                // Act
                var context2 = await network.GetUgcFileDetailsAsync(context);

                // Assert
                var ex = context2.UgcFileDetailsException;
                Assert.IsNotNull(ex);
                Assert.AreEqual(HttpStatusCode.BadRequest, ex.StatusCode);
            }

            [TestMethod]
            public async Task SetsUgcFileDetails()
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
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(ugcId);

                // Act
                var context2 = await network.GetUgcFileDetailsAsync(context);

                // Assert
                Assert.AreEqual("http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/", context2.UgcFileDetails.Data.Url);
            }
        }

        [TestClass]
        public class GetUgcFileAsyncMethod
        {
            [TestMethod]
            public async Task GetUgcFileAsyncThrowsHttpRequestStatusException_SetsUgcFileException()
            {
                // Arrange
                var appId = 247080U;
                var ugcId = 849347241492683863;
                var cancellationToken = CancellationToken.None;
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var ugcHttpClientHandler = new MockHttpMessageHandler();
                ugcHttpClientHandler.When("*").Respond(HttpStatusCode.BadRequest, new StringContent(""));
                var ugcHttpClientHandlers = HttpClientFactory.CreatePipeline(ugcHttpClientHandler, new DelegatingHandler[]
                {
                    new HttpErrorHandler(),
                });
                var ugcHttpClient = new UgcHttpClient(ugcHttpClientHandlers);
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
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(ugcId) { UgcFileDetails = ugcFileDetails };

                // Act
                var context2 = await network.GetUgcFileAsync(context);

                // Assert
                var ex = context2.UgcFileException;
                Assert.IsNotNull(ex);
                Assert.AreEqual(HttpStatusCode.BadRequest, ex.StatusCode);
            }

            [TestMethod]
            public async Task SetsUgcFile()
            {
                // Arrange
                var appId = 247080U;
                var ugcId = 849347241492683863;
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
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(ugcId) { UgcFileDetails = ugcFileDetails };

                // Act
                var context2 = await network.GetUgcFileAsync(context);

                // Assert
                Assert.AreEqual(999, context2.UgcFile.Length);
            }
        }

        [TestClass]
        public class ReadReplayDataMethod
        {
            [TestMethod]
            public void SetsReplayData()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var rawReplayData = Resources.RawReplayData;
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(ugcId) { UgcFile = rawReplayData };

                // Act
                var context2 = ReplayDataflowNetwork.ReadReplayData(context);

                // Assert
                Assert.AreEqual(94, context2.ReplayData.Header.Version);
                Assert.AreEqual("LIGHT MINOTAUR", context2.ReplayData.Header.KilledBy);
            }
        }

        [TestClass]
        public class CreateReplayMethod
        {
            [TestMethod]
            public void SetsReplay()
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
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(ugcId) { ReplayData = replayData };

                // Act
                var context2 = ReplayDataflowNetwork.CreateReplay(context);

                // Assert
                Assert.IsInstanceOfType(context2.Replay, typeof(Replay));
            }
        }

        [TestClass]
        public class CreateReplayWithoutUgcFileMethod
        {
            [TestMethod]
            public async Task SetsReplayWithErrorCodeSetTo2xxx()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var mockHandler = new MockHttpMessageHandler();
                mockHandler.When("*").Respond(HttpStatusCode.NotFound, new StringContent(""));
                var handler = new HttpMessageHandlerAdapter(new HttpErrorHandler { InnerHandler = mockHandler });
                HttpRequestStatusException httpEx = null;
                try
                {
                    await handler.PublicSendAsync(Mock.Of<HttpRequestMessage>());
                }
                catch (HttpRequestStatusException ex)
                {
                    httpEx = ex;
                }
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(ugcId) { UgcFileException = httpEx };

                // Act
                var context2 = ReplayDataflowNetwork.CreateReplayWithoutUgcFile(context);

                // Assert
                var replay = context2.Replay;
                Assert.IsInstanceOfType(replay, typeof(Replay));
                Assert.AreEqual(2404, replay.ErrorCode);
            }
        }

        [TestClass]
        public class GetReplayMethod
        {
            [TestMethod]
            public void ReturnsReplay()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var replay = new Replay();
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(ugcId) { Replay = replay };

                // Act
                var replay2 = ReplayDataflowNetwork.GetReplay(context);

                // Assert
                Assert.AreSame(replay, replay2);
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
                var ugcId = 849347241492683863;
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
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(ugcId)
                {
                    ReplayData = replayData,
                    Replay = replay,
                };

                // Act
                await network.StoreUgcFileAsync(context);

                // Assert
                mockBlob.Verify(b => b.UploadFromStreamAsync(It.IsAny<Stream>(), cancellationToken), Times.Once);
            }
        }
    }
}
