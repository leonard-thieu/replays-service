﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Polly;
using RichardSzalay.MockHttp;
using toofz.Data;
using toofz.NecroDancer.Replays;
using toofz.Services.ReplaysService.Tests.Properties;
using toofz.Steam;
using toofz.Steam.WebApi;
using toofz.Steam.WebApi.ISteamRemoteStorage;
using toofz.Steam.Workshop;
using Xunit;
using ReplayDataflowContext = toofz.Services.ReplaysService.ReplayDataflowNetwork.ReplayDataflowContext;

namespace toofz.Services.ReplaysService.Tests
{
    public class ReplayDataflowNetworkTests
    {
        public ReplayDataflowNetworkTests()
        {
            steamWebApiClient = mockSteamWebApiClient.Object;
            ugcHttpClient = mockUgcHttpClient.Object;
            directory = mockDirectory.Object;

            network = new ReplayDataflowNetwork(appId, steamWebApiClient, ugcHttpClient, directory, cancellationToken);
        }

        private readonly TelemetryClient telemetryClient = new TelemetryClient();
        private readonly uint appId = 247080;
        private readonly Mock<ISteamWebApiClient> mockSteamWebApiClient = new Mock<ISteamWebApiClient>();
        private readonly ISteamWebApiClient steamWebApiClient;
        private readonly Mock<IUgcHttpClient> mockUgcHttpClient = new Mock<IUgcHttpClient>();
        private readonly IUgcHttpClient ugcHttpClient;
        private readonly Mock<ICloudBlobDirectory> mockDirectory = new Mock<ICloudBlobDirectory>();
        private readonly ICloudBlobDirectory directory;
        private readonly CancellationToken cancellationToken = default;
        private readonly ReplayDataflowNetwork network;

        public class Constructor
        {
            [DisplayFact]
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

        public class SendAsyncMethod : ReplayDataflowNetworkTests
        {
            [DisplayFact]
            public async Task AcceptsItem()
            {
                // Arrange
                var replay = new Replay { ReplayId = 849347241492683863 };

                // Act
                var accepted = await network.SendAsync(replay, cancellationToken);

                // Assert
                Assert.True(accepted);
            }
        }

        public class CompleteMethod : ReplayDataflowNetworkTests
        {
            [DisplayFact]
            public async Task SignalsCompletion()
            {
                // Arrange -> Act
                network.Complete();

                // Assert
                var completion = network.Completion;
                await completion;
                Assert.True(completion.IsCompleted);
            }
        }

        public class GetUgcFileDetailsAsyncMethod : ReplayDataflowNetworkTests
        {
            [DisplayFact(
                nameof(ISteamWebApiClient.GetUgcFileDetailsAsync),
                nameof(HttpRequestStatusException),
                nameof(ReplayDataflowContext.UgcFileDetailsException))]
            public async Task GetUgcFileDetailsAsyncThrowsHttpRequestStatusException_SetsUgcFileDetailsException()
            {
                // Arrange
                var ugcId = 849347241492683863;
                mockSteamWebApiClient
                    .Setup(s => s.GetUgcFileDetailsAsync(appId, ugcId, It.IsAny<IProgress<long>>(), cancellationToken))
                    .ThrowsAsync(new HttpRequestStatusException(HttpStatusCode.BadRequest, new Uri("http://example.org")));
                var replay = new Replay { ReplayId = ugcId };
                var context = new ReplayDataflowContext(replay);

                // Act
                var context2 = await network.GetUgcFileDetailsAsync(context);

                // Assert
                var ex = context2.UgcFileDetailsException;
                Assert.NotNull(ex);
                Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            }

            [DisplayFact(nameof(ReplayDataflowContext.UgcFileDetails))]
            public async Task SetsUgcFileDetails()
            {
                // Arrange
                var ugcId = 849347241492683863;
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
                var replay = new Replay { ReplayId = ugcId };
                var context = new ReplayDataflowContext(replay);

                // Act
                var context2 = await network.GetUgcFileDetailsAsync(context);

                // Assert
                Assert.Equal("http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/", context2.UgcFileDetails.Data.Url);
            }
        }

        public class GetUgcFileAsyncMethod : ReplayDataflowNetworkTests
        {
            [DisplayFact(
                nameof(IUgcHttpClient.GetUgcFileAsync),
                nameof(HttpRequestStatusException),
                nameof(ReplayDataflowContext.UgcFileException))]
            public async Task GetUgcFileAsyncThrowsHttpRequestStatusException_SetsUgcFileException()
            {
                // Arrange
                var ugcFileUrl = "http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/";
                mockUgcHttpClient
                    .Setup(c => c.GetUgcFileAsync(ugcFileUrl, It.IsAny<IProgress<long>>(), cancellationToken))
                    .ThrowsAsync(new HttpRequestStatusException(HttpStatusCode.BadRequest, new Uri("http://example.org")));

                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var ugcFileDetails = new UgcFileDetailsEnvelope
                {
                    Data = new UgcFileDetails
                    {
                        FileName = "DLC HARDCORE All Chars DLC_PROD_SCORE134377_ZONE11_LEVEL1",
                        Url = ugcFileUrl,
                        Size = 999,
                    },
                };
                var context = new ReplayDataflowContext(replay) { UgcFileDetails = ugcFileDetails };

                // Act
                var context2 = await network.GetUgcFileAsync(context);

                // Assert
                var ex = context2.UgcFileException;
                Assert.NotNull(ex);
                Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            }

            [DisplayFact(nameof(ReplayDataflowContext.UgcFile))]
            public async Task SetsUgcFile()
            {
                // Arrange
                var ugcFileUrl = "http://cloud-3.steamusercontent.com/ugc/849347241492683863/9AC1027041B31DBC1EED3E1A709D6930D7165BEA/";
                mockUgcHttpClient
                    .Setup(c => c.GetUgcFileAsync(ugcFileUrl, It.IsAny<IProgress<long>>(), cancellationToken))
                    .ReturnsAsync(Resources.RawReplayData);

                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var ugcFileDetails = new UgcFileDetailsEnvelope
                {
                    Data = new UgcFileDetails
                    {
                        FileName = "DLC HARDCORE All Chars DLC_PROD_SCORE134377_ZONE11_LEVEL1",
                        Url = ugcFileUrl,
                        Size = 999,
                    },
                };
                var context = new ReplayDataflowContext(replay) { UgcFileDetails = ugcFileDetails };

                // Act
                var context2 = await network.GetUgcFileAsync(context);

                // Assert
                Assert.Equal(999, context2.UgcFile.Length);
            }
        }

        public class ReadReplayDataMethod
        {
            [DisplayFact(nameof(ReplayDataflowContext.ReplayData))]
            public void SetsReplayData()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var rawReplayData = Resources.RawReplayData;
                var context = new ReplayDataflowContext(replay) { UgcFile = rawReplayData };

                // Act
                var context2 = ReplayDataflowNetwork.ReadReplayData(context);

                // Assert
                Assert.Equal(94, context2.ReplayData.Version);
                Assert.Equal("LIGHT MINOTAUR", context2.ReplayData.KilledBy);
            }
        }

        public class CreateReplayMethod
        {
            [DisplayFact(nameof(ReplayDataflowContext.Replay))]
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
                var context = new ReplayDataflowContext(replay) { ReplayData = replayData };

                // Act
                var context2 = ReplayDataflowNetwork.UpdateReplay(context);

                // Assert
                Assert.IsAssignableFrom<Replay>(context2.Replay);
            }
        }

        public class CreateReplayWithoutUgcFileDetailsMethod
        {
            [DisplayFact(nameof(ReplayDataflowContext.Replay), nameof(Replay.ErrorCode))]
            public void SetsReplayWithErrorCodeSetTo1xxx()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var requestUri = new Uri("http://localhost/");
                var httpEx = new HttpRequestStatusException(HttpStatusCode.NotFound, requestUri);
                var context = new ReplayDataflowContext(replay) { UgcFileDetailsException = httpEx };

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
            [DisplayFact(nameof(ReplayDataflowContext.Replay), nameof(Replay.ErrorCode))]
            public void SetsReplayWithErrorCodeSetTo2xxx()
            {
                // Arrange
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var requestUri = new Uri("http://localhost/");
                var httpEx = new HttpRequestStatusException(HttpStatusCode.NotFound, requestUri);
                var context = new ReplayDataflowContext(replay) { UgcFileException = httpEx };

                // Act
                var context2 = ReplayDataflowNetwork.OnUgcFileError(context);

                // Assert
                var replay2 = context2.Replay;
                Assert.IsAssignableFrom<Replay>(replay2);
                Assert.Equal(2404, replay2.ErrorCode);
            }
        }

        public class StoreUgcFileAsyncMethod : ReplayDataflowNetworkTests
        {
            [DisplayFact]
            public async Task StoresUgcFile()
            {
                // Arrange
                var mockBlob = new Mock<ICloudBlockBlob>();
                mockBlob.SetupGet(b => b.Properties).Returns(new BlobProperties());
                mockBlob.SetupGet(b => b.Uri).Returns(new Uri("http://example.org/"));
                var blob = mockBlob.Object;
                mockDirectory.Setup(d => d.GetBlockBlobReference(It.IsAny<string>())).Returns(blob);
                var ugcId = 849347241492683863;
                var replay = new Replay { ReplayId = ugcId };
                var replayData = new ReplayData();
                var context = new ReplayDataflowContext(replay) { ReplayData = replayData };

                // Act
                await network.StoreUgcFileAsync(context);

                // Assert
                mockBlob.Verify(b => b.UploadFromStreamAsync(It.IsAny<Stream>(), cancellationToken), Times.Once);
            }
        }

        public class IntegrationTests
        {
            [DisplayFact]
            public async Task UgcFileDetailsNotFound_DoesNotHang()
            {
                // Arrange
                var steamWebApiClientHandler = new MockHttpMessageHandler();
                steamWebApiClientHandler
                    .When("http://localhost/")
                    .Respond(HttpStatusCode.NotFound, new StringContent(Resources.UgcFileDetails_847096111522125255_NotFound, Encoding.UTF8, "application/json"));
                var steamWebApiClientRetryPolicy = Policy
                    .Handle<Exception>(SteamWebApiClient.IsTransient)
                    .RetryAsync();
                var steamWebApiClientHandlers = HttpClientFactory.CreatePipeline(steamWebApiClientHandler, new DelegatingHandler[]
                {
                    new TransientFaultHandler(steamWebApiClientRetryPolicy),
                });
                var telemetryClient = new TelemetryClient();
                var steamWebApiClient = new SteamWebApiClient(steamWebApiClientHandlers, telemetryClient)
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
