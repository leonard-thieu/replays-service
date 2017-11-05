using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using RichardSzalay.MockHttp;
using toofz.NecroDancer.Leaderboards.ReplaysService.Tests.Properties;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamRemoteStorage;
using toofz.NecroDancer.Replays;
using toofz.Services;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    public class ReplayDataflowNetworkTests
    {
        public class Constructor
        {
            [Fact]
            public void SteamWebApiClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                ISteamWebApiClient steamWebApiClient = null;
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                Assert.Throws<ArgumentNullException>(() =>
                {
                    new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                });
            }

            [Fact]
            public void AppIdIsNotAPositiveInteger_ThrowsArgumentOutOfRangeException()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 0U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                });
            }

            [Fact]
            public void UgcHttpClientIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                IUgcHttpClient ugcHttpClient = null;
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                Assert.Throws<ArgumentNullException>(() =>
                {
                    new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                });
            }

            [Fact]
            public void DirectoryIsNull_ThrowsArgumentNullException()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                ICloudBlobDirectory directory = null;
                var cancellationToken = CancellationToken.None;

                // Act -> Assert
                Assert.Throws<ArgumentNullException>(() =>
                {
                    new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                });
            }

            [Fact]
            public void ReturnsInstance()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;

                // Act
                var network = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);

                // Assert
                Assert.IsAssignableFrom<ReplayDataflowNetwork>(network);
            }
        }

        public class SendAsyncMethod
        {
            [Fact]
            public async Task AcceptsItem()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;
                var network = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                var replay = new Replay { ReplayId = 849347241492683863 };

                // Act
                var accepted = await network.SendAsync(replay, cancellationToken);

                // Assert
                Assert.True(accepted);
            }
        }

        public class CompleteMethod
        {
            [Fact]
            public async Task SignalsCompletion()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;
                var network = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);

                // Act
                network.Complete();

                // Assert
                var completion = network.Completion;
                await completion;
                Assert.True(completion.IsCompleted);
            }
        }

        public class GetUgcFileDetailsAsyncMethod
        {
            [Fact]
            public async Task GetUgcFileDetailsAsyncThrowsHttpRequestStatusException_SetsUgcFileDetailsException()
            {
                // Arrange
                var appId = 247080U;
                var replay = new Replay { ReplayId = 849347241492683863 };
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
                var network = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(replay);

                // Act
                var context2 = await network.GetUgcFileDetailsAsync(context);

                // Assert
                var ex = context2.UgcFileDetailsException;
                Assert.NotNull(ex);
                Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            }

            [Fact]
            public async Task SetsUgcFileDetails()
            {
                // Arrange
                var appId = 247080U;
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var cancellationToken = CancellationToken.None;
                var mockSteamWebApiClient = new Mock<ISteamWebApiClient>();
                mockSteamWebApiClient
                    .Setup(s => s.GetUgcFileDetailsAsync(appId, ugcId, It.IsAny<IProgress<long>>(), cancellationToken))
                    .ReturnsAsync(new UgcFileDetailsEnvelope
                    {
                        Data = new UgcFileDetails
                        {
                            FileName = "DLC HARDCORE All Chars DLC_PROD_SCORE134377_ZONE11_LEVEL1",
                            Url = "http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/",
                            Size = 999,
                        },
                    });
                var steamWebApiClient = mockSteamWebApiClient.Object;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var network = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(replay);

                // Act
                var context2 = await network.GetUgcFileDetailsAsync(context);

                // Assert
                Assert.Equal("http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/", context2.UgcFileDetails.Data.Url);
            }
        }

        public class GetUgcFileAsyncMethod
        {
            [Fact]
            public async Task GetUgcFileAsyncThrowsHttpRequestStatusException_SetsUgcFileException()
            {
                // Arrange
                var appId = 247080U;
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
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
                var network = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                var ugcFileDetails = new UgcFileDetailsEnvelope
                {
                    Data = new UgcFileDetails
                    {
                        FileName = "DLC HARDCORE All Chars DLC_PROD_SCORE134377_ZONE11_LEVEL1",
                        Url = "http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/",
                        Size = 999,
                    },
                };
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(replay) { UgcFileDetails = ugcFileDetails };

                // Act
                var context2 = await network.GetUgcFileAsync(context);

                // Assert
                var ex = context2.UgcFileException;
                Assert.NotNull(ex);
                Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            }

            [Fact]
            public async Task SetsUgcFile()
            {
                // Arrange
                var appId = 247080U;
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var cancellationToken = CancellationToken.None;
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var mockUgcHttpClient = new Mock<IUgcHttpClient>();
                mockUgcHttpClient
                    .Setup(c => c.GetUgcFileAsync("http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/", It.IsAny<IProgress<long>>(), cancellationToken))
                    .ReturnsAsync(Resources.RawReplayData);
                var ugcHttpClient = mockUgcHttpClient.Object;
                var directory = Mock.Of<ICloudBlobDirectory>();
                var network = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                var ugcFileDetails = new UgcFileDetailsEnvelope
                {
                    Data = new UgcFileDetails
                    {
                        FileName = "DLC HARDCORE All Chars DLC_PROD_SCORE134377_ZONE11_LEVEL1",
                        Url = "http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/",
                        Size = 999,
                    },
                };
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(replay) { UgcFileDetails = ugcFileDetails };

                // Act
                var context2 = await network.GetUgcFileAsync(context);

                // Assert
                Assert.Equal(999, context2.UgcFile.Length);
            }
        }

        public class ReadReplayDataMethod
        {
            [Fact]
            public void SetsReplayData()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var rawReplayData = Resources.RawReplayData;
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(replay) { UgcFile = rawReplayData };

                // Act
                var context2 = ReplayDataflowNetwork.ReadReplayData(context);

                // Assert
                Assert.Equal(94, context2.ReplayData.Version);
                Assert.Equal("LIGHT MINOTAUR", context2.ReplayData.KilledBy);
            }
        }

        public class CreateReplayMethod
        {
            [Fact]
            public void SetsReplay()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var replayData = new ReplayData
                {
                    Version = 94,
                    KilledBy = "LIGHT MINOTAUR",
                };
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(replay) { ReplayData = replayData };

                // Act
                var context2 = ReplayDataflowNetwork.UpdateReplay(context);

                // Assert
                Assert.IsAssignableFrom<Replay>(context2.Replay);
            }
        }

        public class CreateReplayWithoutUgcFileDetailsMethod
        {
            [Fact]
            public void SetsReplayWithErrorCodeSetTo1xxx()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var requestUri = new Uri("http://localhost/");
                var httpEx = new HttpRequestStatusException(HttpStatusCode.NotFound, requestUri);
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(replay) { UgcFileDetailsException = httpEx };

                // Act
                var context2 = ReplayDataflowNetwork.OnUgcFileDetailsError(context);

                // Assert
                var replay2 = context2.Replay;
                Assert.IsAssignableFrom<Replay>(replay2);
                Assert.Equal(1404, replay2.ErrorCode);
            }
        }

        public class CreateReplayWithoutUgcFileMethod
        {
            [Fact]
            public void SetsReplayWithErrorCodeSetTo2xxx()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var requestUri = new Uri("http://localhost/");
                var httpEx = new HttpRequestStatusException(HttpStatusCode.NotFound, requestUri);
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(replay) { UgcFileException = httpEx };

                // Act
                var context2 = ReplayDataflowNetwork.OnUgcFileError(context);

                // Assert
                var replay2 = context2.Replay;
                Assert.IsAssignableFrom<Replay>(replay2);
                Assert.Equal(2404, replay2.ErrorCode);
            }
        }

        public class StoreUgcFileAsyncMethod
        {
            [Fact]
            public async Task StoresUgcFile()
            {
                // Arrange
                var steamWebApiClient = Mock.Of<ISteamWebApiClient>();
                var appId = 247080U;
                var ugcId = 849347241492683863;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var mockBlob = new Mock<ICloudBlockBlob>();
                mockBlob.SetupGet(b => b.Properties).Returns(new BlobProperties());
                mockBlob.SetupGet(b => b.Uri).Returns(new Uri("http://example.org/"));
                var blob = mockBlob.Object;
                var mockDirectory = new Mock<ICloudBlobDirectory>();
                mockDirectory.Setup(d => d.GetBlockBlobReference(It.IsAny<string>())).Returns(blob);
                var directory = mockDirectory.Object;
                var cancellationToken = CancellationToken.None;
                var network = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                var replayData = new ReplayData();
                var replay = new Replay { ReplayId = ugcId };
                var context = new ReplayDataflowNetwork.ReplayDataflowContext(replay) { ReplayData = replayData };

                // Act
                await network.StoreUgcFileAsync(context);

                // Assert
                mockBlob.Verify(b => b.UploadFromStreamAsync(It.IsAny<Stream>(), cancellationToken), Times.Once);
            }
        }

        public class IntegrationTests
        {
            [Fact]
            public async Task UgcFileDetailsNotFound_DoesNotHang()
            {
                // Arrange
                var steamWebApiClientHandler = new MockHttpMessageHandler();
                steamWebApiClientHandler
                    .When("http://localhost/")
                    .Respond(HttpStatusCode.NotFound, new StringContent(Resources.UgcFileDetails_847096111522125255_NotFound, Encoding.UTF8, "application/json"));
                var steamWebApiClientHandlers = HttpClientFactory.CreatePipeline(steamWebApiClientHandler, new DelegatingHandler[]
                {
                    new SteamWebApiTransientFaultHandler(),
                });
                var steamWebApiClient = new SteamWebApiClient(steamWebApiClientHandlers)
                {
                    SteamWebApiKey = "mySteamWebApiKey",
                };
                var appId = 247080U;
                var ugcHttpClient = Mock.Of<IUgcHttpClient>();
                var directory = Mock.Of<ICloudBlobDirectory>();
                var cancellationToken = CancellationToken.None;
                var network = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
                var ugcId = 847096111522125255;
                var replay = new Replay { ReplayId = ugcId };
                await network.SendAsync(replay, cancellationToken);
                network.Complete();

                // Act
                var completion = network.Completion;

                // Assert
                await completion;
                steamWebApiClientHandler.VerifyNoOutstandingRequest();
            }
        }
    }
}
