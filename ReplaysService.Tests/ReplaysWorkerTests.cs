﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using RichardSzalay.MockHttp;
using toofz.NecroDancer.Leaderboards.ReplaysService.Properties;
using toofz.NecroDancer.Leaderboards.ReplaysService.Tests.Properties;
using toofz.NecroDancer.Leaderboards.Steam;
using toofz.NecroDancer.Leaderboards.Steam.WebApi;
using toofz.NecroDancer.Leaderboards.Steam.WebApi.ISteamRemoteStorage;
using toofz.TestsShared;
using Xunit;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    public class ReplaysWorkerTests
    {
        public ReplaysWorkerTests()
        {
            worker = new ReplaysWorker(
                appId,
                mockDb.Object,
                mockSteamWebApiClient.Object,
                mockUgcHttpClient.Object,
                mockDirectoryFactory.Object,
                mockStoreClient.Object,
                telemetryClient);
        }

        private readonly uint appId = 247080;
        private readonly Mock<ILeaderboardsContext> mockDb = new Mock<ILeaderboardsContext>();
        private readonly Mock<ISteamWebApiClient> mockSteamWebApiClient = new Mock<ISteamWebApiClient>();
        private readonly Mock<IUgcHttpClient> mockUgcHttpClient = new Mock<IUgcHttpClient>();
        private readonly Mock<ICloudBlobDirectoryFactory> mockDirectoryFactory = new Mock<ICloudBlobDirectoryFactory>();
        private readonly Mock<ILeaderboardsStoreClient> mockStoreClient = new Mock<ILeaderboardsStoreClient>();
        private readonly TelemetryClient telemetryClient = new TelemetryClient();
        private readonly ReplaysWorker worker;

        public class GetReplaysAsyncMethod : ReplaysWorkerTests
        {
            private readonly CancellationToken cancellationToken = CancellationToken.None;

            [Fact]
            public async Task ReturnsReplays()
            {
                // Arrange
                var mockDbReplays = new MockDbSet<Replay>();
                mockDb.Setup(d => d.Replays).Returns(mockDbReplays.Object);
                var limit = 20;

                // Act
                var replays = await worker.GetReplaysAsync(limit, cancellationToken);

                // Assert
                Assert.IsAssignableFrom<IEnumerable<Replay>>(replays);
            }
        }

        public class UpdateReplaysAsyncMethod : ReplaysWorkerTests
        {
            public UpdateReplaysAsyncMethod()
            {
                var mockBlob = new Mock<ICloudBlockBlob>();
                mockBlob.SetupGet(b => b.Properties).Returns(new BlobProperties());
                mockBlob.SetupGet(b => b.Uri).Returns(new Uri("http://example.org/"));

                var mockDirectory = new Mock<ICloudBlobDirectory>();
                mockDirectory.Setup(d => d.GetBlockBlobReference(It.IsAny<string>())).Returns(mockBlob.Object);

                mockDirectoryFactory.Setup(f => f.GetCloudBlobDirectoryAsync("crypt", "replays", cancellationToken)).ReturnsAsync(mockDirectory.Object);
            }

            private readonly List<Replay> replays = new List<Replay>();
            private readonly CancellationToken cancellationToken = CancellationToken.None;


            [Fact]
            public async Task UpdatesReplays()
            {
                // Arrange
                mockSteamWebApiClient
                    .Setup(c => c.GetUgcFileDetailsAsync(appId, It.IsAny<long>(), It.IsAny<IProgress<long>>(), cancellationToken))
                    .ReturnsAsync(new UgcFileDetailsEnvelope { Data = new UgcFileDetails() });
                replays.Add(new Replay());

                // Act
                await worker.UpdateReplaysAsync(replays, cancellationToken);

                // Assert
                Assert.IsAssignableFrom<IEnumerable<Replay>>(replays);
            }
        }

        public class StoreReplaysAsyncMethod : ReplaysWorkerTests
        {
            private readonly List<Replay> replays = new List<Replay>();
            private readonly CancellationToken cancellationToken = CancellationToken.None;


            [Fact]
            public async Task StoresReplays()
            {
                // Arrange
                mockStoreClient
                    .Setup(c => c.BulkUpsertAsync(replays, cancellationToken))
                    .ReturnsAsync(replays.Count);

                // Act
                await worker.StoreReplaysAsync(replays, cancellationToken);

                // Assert
                mockStoreClient.Verify(c => c.BulkUpsertAsync(replays, cancellationToken), Times.Once);
            }
        }

        [Trait("Category", "Uses Azure Storage Emulator")]
        [Collection(AzureStorageCollection.Name)]
        [Trait("Category", "Uses SQL Server")]
        public class IntegrationTests : IDisposable
        {
            public IntegrationTests()
            {
                var connectionString = DatabaseHelper.GetConnectionString();
                db = new LeaderboardsContext(connectionString);
                db.Database.Delete(); // Make sure it really dropped - needed for dirty database
                db.Database.Initialize(force: true);
                storeClient = new LeaderboardsStoreClient(connectionString);
            }

            private readonly LeaderboardsContext db;
            private readonly ILeaderboardsStoreClient storeClient;

            public void Dispose()
            {
                storeClient.Dispose();
                db.Database.Delete();
                db.Dispose();
            }

            [Fact]
            public async Task EndToEnd()
            {
                // Arrange
                var log = Mock.Of<ILog>();
                var telemetryClient = new TelemetryClient();
                var settings = Mock.Of<IReplaysSettings>();

                #region LeaderboardsContext

                var replayIds = new[]
                {
                    844845073340377377,
                    844845073340436306,
                    844845073340472702,
                    844845073340489317,
                    844845073340511612,
                    844845073340512126,
                    844845073340539302,
                    844845073340574779,
                    844845073340582264,
                    844845073340618972,
                    844845073340654449,
                    844845073340658917,
                    844845073340661010,
                    844845073340712746,
                    844845073340796353,
                    844845073340843735,
                    845970274592369232,
                    845970274592383940,
                    845970274592402923,
                    845970274592404139,
                    845970274592417527,
                    845970274592539966,
                    845970274592540128,
                    845970274592641888,
                    845970274592656129,
                    845970274592667508,
                    845970274592678894,
                    845970274592679004,
                    845970274592783564,
                    845970274592783747,
                    845970274592823214,
                    845970351216224430,
                    845970351216237024,
                    845970351216294057,
                    845970351216353794,
                    845970351216404327,
                    845970351216460106,
                    845970351216468628,
                    845970351216482197,
                    845970351216614550,
                    845970351216627104,
                    845970351216647618,
                    845970351216663674,
                    845970351216677651,
                    845970351216722085,
                    845970351216735376,
                    845970351216743053,
                    845970351216748528,
                    845970351216837544,
                    845970351216845676,
                    845970351216964529,
                    845970351216998233,
                    845970351217037549,
                    845970351217043243,
                    845970351217061481,
                    845970351217127622,
                    845970351217136579,
                    845970351217310845,
                    845970351217341480,
                    845970351217499797,
                };
                foreach (var replayId in replayIds)
                {
                    db.Replays.Add(new Replay { ReplayId = replayId });
                    db.SaveChanges();
                }

                #endregion

                #region SteamWebApiClient

                var steamWebApiClientHandler = new MockHttpMessageHandler();
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340377377, FullCycleResources.UgcFileDetails_844845073340377377);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340436306, FullCycleResources.UgcFileDetails_844845073340436306);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340472702, FullCycleResources.UgcFileDetails_844845073340472702);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340489317, FullCycleResources.UgcFileDetails_844845073340489317);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340511612, FullCycleResources.UgcFileDetails_844845073340511612);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340512126, FullCycleResources.UgcFileDetails_844845073340512126);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340539302, FullCycleResources.UgcFileDetails_844845073340539302);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340574779, FullCycleResources.UgcFileDetails_844845073340574779);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340582264, FullCycleResources.UgcFileDetails_844845073340582264);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340618972, FullCycleResources.UgcFileDetails_844845073340618972);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340654449, FullCycleResources.UgcFileDetails_844845073340654449);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340658917, FullCycleResources.UgcFileDetails_844845073340658917);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340661010, FullCycleResources.UgcFileDetails_844845073340661010);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340712746, FullCycleResources.UgcFileDetails_844845073340712746);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340796353, FullCycleResources.UgcFileDetails_844845073340796353);
                steamWebApiClientHandler.RespondWithUgcFileDetails(844845073340843735, FullCycleResources.UgcFileDetails_844845073340843735);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592369232, FullCycleResources.UgcFileDetails_845970274592369232);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592383940, FullCycleResources.UgcFileDetails_845970274592383940);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592402923, FullCycleResources.UgcFileDetails_845970274592402923);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592404139, FullCycleResources.UgcFileDetails_845970274592404139);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592417527, FullCycleResources.UgcFileDetails_845970274592417527);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592539966, FullCycleResources.UgcFileDetails_845970274592539966);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592540128, FullCycleResources.UgcFileDetails_845970274592540128);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592641888, FullCycleResources.UgcFileDetails_845970274592641888);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592656129, FullCycleResources.UgcFileDetails_845970274592656129);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592667508, FullCycleResources.UgcFileDetails_845970274592667508);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592678894, FullCycleResources.UgcFileDetails_845970274592678894);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592679004, FullCycleResources.UgcFileDetails_845970274592679004);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592783564, FullCycleResources.UgcFileDetails_845970274592783564);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592783747, FullCycleResources.UgcFileDetails_845970274592783747);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970274592823214, FullCycleResources.UgcFileDetails_845970274592823214);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216224430, FullCycleResources.UgcFileDetails_845970351216224430);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216237024, FullCycleResources.UgcFileDetails_845970351216237024);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216294057, FullCycleResources.UgcFileDetails_845970351216294057);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216353794, FullCycleResources.UgcFileDetails_845970351216353794);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216404327, FullCycleResources.UgcFileDetails_845970351216404327);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216460106, FullCycleResources.UgcFileDetails_845970351216460106);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216468628, FullCycleResources.UgcFileDetails_845970351216468628);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216482197, FullCycleResources.UgcFileDetails_845970351216482197);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216614550, FullCycleResources.UgcFileDetails_845970351216614550);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216627104, FullCycleResources.UgcFileDetails_845970351216627104);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216647618, FullCycleResources.UgcFileDetails_845970351216647618);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216663674, FullCycleResources.UgcFileDetails_845970351216663674);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216677651, FullCycleResources.UgcFileDetails_845970351216677651);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216722085, FullCycleResources.UgcFileDetails_845970351216722085);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216735376, FullCycleResources.UgcFileDetails_845970351216735376);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216743053, FullCycleResources.UgcFileDetails_845970351216743053);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216748528, FullCycleResources.UgcFileDetails_845970351216748528);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216837544, FullCycleResources.UgcFileDetails_845970351216837544);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216845676, FullCycleResources.UgcFileDetails_845970351216845676);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216964529, FullCycleResources.UgcFileDetails_845970351216964529);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351216998233, FullCycleResources.UgcFileDetails_845970351216998233);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351217037549, FullCycleResources.UgcFileDetails_845970351217037549);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351217043243, FullCycleResources.UgcFileDetails_845970351217043243);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351217061481, FullCycleResources.UgcFileDetails_845970351217061481);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351217127622, FullCycleResources.UgcFileDetails_845970351217127622);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351217136579, FullCycleResources.UgcFileDetails_845970351217136579);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351217310845, FullCycleResources.UgcFileDetails_845970351217310845);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351217341480, FullCycleResources.UgcFileDetails_845970351217341480);
                steamWebApiClientHandler.RespondWithUgcFileDetails(845970351217499797, FullCycleResources.UgcFileDetails_845970351217499797);
                var steamWebApiClientHandlers = KernelConfig.CreateSteamWebApiClientHandler(steamWebApiClientHandler, log, telemetryClient);
                var steamWebApiClient = new SteamWebApiClient(steamWebApiClientHandlers, telemetryClient) { SteamWebApiKey = "mySteamWebApiKey" };

                #endregion

                #region UgcHttpClient

                var ugcHttpClientHandler = new MockHttpMessageHandler();
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340377377/ECD1D490139C0EE48C876964B16AF3E1EEEA854E/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_PROD_SCORE52_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340436306/3CEADC5265EA982F1817C7D69223350AA002D3E8/").Respond(new ByteArrayContent(FullCycleResources._12_9_2017_PROD_SCORE51_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340472702/9C96265F0CA963484156A6EE2ACB0F16899A9E23/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_Story_Mode_PROD_SCORE121_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340489317/C2CE08E0AEB55B105E7BA7DEBB14BD4E220D3534/").Respond(new ByteArrayContent(FullCycleResources.DLC_12_9_2017_PROD_SCORE210_ZONE1_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340511612/DD3141F6DE7BBDD77D162EC46FF4FBF4E192D205/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_Story_Mode_PROD_SCORE143_ZONE1_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340512126/0E222A0D8AE7C7E32B4CDD069569AD3881D17B04/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_SEEDED_PROD_SCORE55_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340539302/07D607C5823871014339D6845F0AED5EFCADCBED/").Respond(new ByteArrayContent(FullCycleResources._12_9_2017_PROD_SCORE31_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340574779/B6F63ACB7884E1FCBDEA70EE9E1442176B2CA82A/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_Nocturna_PROD_SCORE488_ZONE2_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340582264/1B0144284FFC0EB9259A95693A7C7DEFCDADC754/").Respond(new ByteArrayContent(FullCycleResources.DLC_12_9_2017_PROD_SCORE83_ZONE1_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340618972/7A8A5CD2B080AE55DD9674D06D899DD208D44B8D/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_Story_Mode_PROD_SCORE27_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340654449/C9A3BC13687F220FBADEA74F2BBA340122F96DDB/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_SEEDED_PROD_SCORE0_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340658917/FCC3498974B716F64FEAB6A0B19A8AB70C2E90D3/").Respond(new ByteArrayContent(FullCycleResources._12_9_2017_PROD_SCORE20_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340661010/19DAD40338806239E5BD1E7CAC0302008F0EAB55/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_Bard_PROD_SCORE102_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340712746/9AA2D3D25C5BB95F35B1F03C87A5DD93567E951F/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_PROD_SCORE191_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340796353/46899C8640A4C035D135234942694F5FE3401AE8/").Respond(new ByteArrayContent(FullCycleResources._12_9_2017_PROD_SCORE173_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/844845073340843735/354E39D111D3DB76C29CD2502EFF73E79B905927/").Respond(new ByteArrayContent(FullCycleResources._12_9_2017_PROD_SCORE25_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592369232/749C620891553E9624F40B4668B51F705CBD040D/").Respond(HttpStatusCode.NotFound, new StringContent(FullCycleResources.UgcFileDetails_845970274592369232_NotFound, Encoding.UTF8, "application/xml"));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592383940/2D407B589507B8C85F33DD71D4DFD56DE245E6A4/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_PROD_SCORE534_ZONE1_LEVEL4));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592402923/0FB453BEEA6F40A00AE5A0AF325D71F1713EB1C3/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_PROD_SCORE312_ZONE1_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592404139/9021181A84A13D9D312E4F181A69E5EFD2E77678/").Respond(new ByteArrayContent(FullCycleResources.DLC_5_9_2017_PROD_SCORE1195_ZONE3_LEVEL4));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592417527/283602BCF733A9F6D5C681CE7A5F2E0250442232/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_MYSTERY_PROD_SCORE278_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592539966/55CF57BC8FB7B11F5E37002899D300DDFF6821F6/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_SEEDED_HARD_PROD_SCORE2668_ZONE5_LEVEL6));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592540128/55CF57BC8FB7B11F5E37002899D300DDFF6821F6/").Respond(new ByteArrayContent(FullCycleResources.DLC_SEEDED_SPEEDRUN_HARD_PROD_SCORE97659881_ZONE5_LEVEL6));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592641888/1FD3D2166B6BCDBAF57913F5B3D09B5FB7D5B98D/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_Melody_MYSTERY_PROD_SCORE3398_ZONE5_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592656129/FE52A001581795F447D5B58DDCA9F6A262DCEDEE/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_MYSTERY_PROD_SCORE487_ZONE1_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592667508/96A74C304BC563D353C20815C944EE47BD7FBBA9/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_PHASING_PROD_SCORE209_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592678894/C3DE3E9BA93C89001A62F4659C77AAD96E0F413E/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_PROD_SCORE643_ZONE4_LEVEL6));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592679004/C3DE3E9BA93C89001A62F4659C77AAD96E0F413E/").Respond(new ByteArrayContent(FullCycleResources.SPEEDRUN_PROD_SCORE98067457_ZONE4_LEVEL6));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592783564/9D6F6B679C594FEC34EB76FC90F00F325692D1D2/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_Bard_PHASING_PROD_SCORE4484_ZONE5_LEVEL6));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592783747/9D6F6B679C594FEC34EB76FC90F00F325692D1D2/").Respond(new ByteArrayContent(FullCycleResources.DLC_SPEEDRUN_Bard_PHASING_PROD_SCORE98017110_ZONE5_LEVEL6));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970274592823214/96A269315EC580FC9959811DB5BBC255EBE607CD/").Respond(new ByteArrayContent(FullCycleResources.DLC_SPEEDRUN_PROD_SCORE97674899_ZONE5_LEVEL6));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216224430/CD863E56B3B2B4885D4D5EDDFCA128B879B504CE/").Respond(new ByteArrayContent(FullCycleResources.DLC_5_9_2017_PROD_SCORE174_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216237024/32D5AF9D0C6AE4964B0AC1DE4854E58CE34A62AC/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_Diamond_RANDOMIZER_PROD_SCORE246_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216294057/64E00BB59C2673D3BCF034E8F692E7E6454AD47C/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_Aria_PROD_SCORE265_ZONE4_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216353794/17A1CA69364D4D93834C0D97CE304BBDFD9ABAE2/").Respond(new ByteArrayContent(FullCycleResources.DLC_6_9_2017_PROD_SCORE4410_ZONE3_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216404327/F7CE30C31124F528AA51D2AFC67D443B5F88D027/").Respond(new ByteArrayContent(FullCycleResources.SPEEDRUN_Bard_PROD_SCORE98525016_ZONE4_LEVEL6));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216460106/0A9A35A3C1D2C716C16CBE1F9D8A6898BCFC2DF3/").Respond(new ByteArrayContent(FullCycleResources._6_9_2017_PROD_SCORE2916_ZONE4_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216468628/2C61F2CEA900AD40E52743936314C88EEC9112B0/").Respond(new ByteArrayContent(FullCycleResources._6_9_2017_PROD_SCORE705_ZONE2_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216482197/5728DE896EB5EB2813613ACDB4A194A9BD3B8347/").Respond(new ByteArrayContent(FullCycleResources.DLC_6_9_2017_PROD_SCORE3892_ZONE5_LEVEL6));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216614550/41771F71E93AF82553ECED3EE2ABA8ADE3373200/").Respond(new ByteArrayContent(FullCycleResources._6_9_2017_PROD_SCORE210_ZONE1_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216627104/95C3D45FDACD49A26378F17548C67CCDB63A73FC/").Respond(new ByteArrayContent(FullCycleResources.DLC_6_9_2017_PROD_SCORE7759_ZONE5_LEVEL5));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216647618/19E802BCBE752F915FA36E892E3578B59CB6C831/").Respond(new ByteArrayContent(FullCycleResources._6_9_2017_PROD_SCORE112_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216663674/D5A79B3E560C1138C8B19E27D5FA212276E50C5B/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_Monk_PROD_SCORE6_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216677651/86C4D185D688BAA380BC62DAC551F4A4663F9D3A/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_Monk_DEATHLESS_PROD_SCORE0_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216722085/E366B1AD454EC0D099AFE72AC9E5F946192D248A/").Respond(new ByteArrayContent(FullCycleResources._6_9_2017_PROD_SCORE186_ZONE1_LEVEL4));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216735376/A338C7F7722A0DA5B61722F88096E8013AEC24CC/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_Story_Mode_PROD_SCORE165_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216743053/EF389C60524DAAC2A86AB4CA2403956857C7E23A/").Respond(new ByteArrayContent(FullCycleResources.HARDCORE_PROD_SCORE272_ZONE2_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216748528/E7EFFA44B0B874D2F0C475B1B5B3BC88864D761B/").Respond(new ByteArrayContent(FullCycleResources._6_9_2017_PROD_SCORE59_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216837544/6A87752F3CD0953982B3EC452E3DC426508600BE/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_RANDOMIZER_PROD_SCORE4034_ZONE4_LEVEL4));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216845676/7669BCAB8BE7A3AF4CACE876B6EAC82C3F6FCF83/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_Aria_PROD_SCORE375_ZONE1_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216964529/45E9DA6C71D26FC0FBB7CBCB1D2F123732188811/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_RANDOMIZER_PROD_SCORE171_ZONE1_LEVEL2));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351216998233/6AEC56ABC474BA319FF1FB19616237A44CBFEF69/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_PROD_SCORE396_ZONE2_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351217037549/4E36367CB159295776BD2B6801FA44CF5C380DA1/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_RANDOMIZER_PROD_SCORE2119_ZONE3_LEVEL4));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351217043243/F8D5AE3C3662E6AE26B1565758702D3E54BB09CF/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_MYSTERY_PROD_SCORE87_ZONE1_LEVEL1));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351217061481/80123E31A2C784D5171D383ED832F44EF5D545F6/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_MYSTERY_PROD_SCORE188_ZONE1_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351217127622/8A20EE5D92DAD7E06BE8C6BC2E6591B8CB38D884/").Respond(new ByteArrayContent(FullCycleResources.DLC_6_9_2017_PROD_SCORE1305_ZONE2_LEVEL4));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351217136579/680F0AF9EBD5D915A2CEC5BAB186FA7EDB4FE6DF/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_Tempo_HARD_PROD_SCORE1320_ZONE1_LEVEL3));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351217310845/AB3A5D78136231A85ADABE91DDE687D38F79E84B/").Respond(new ByteArrayContent(FullCycleResources.DLC_SPEEDRUN_PROD_SCORE97460146_ZONE5_LEVEL6));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351217341480/1D2EADAFE681E8E801E8CAB0B1AAEE101A3FADB0/").Respond(new ByteArrayContent(FullCycleResources.DLC_6_9_2017_PROD_SCORE5982_ZONE3_LEVEL4));
                ugcHttpClientHandler.When("http://cloud-3.steamusercontent.com/ugc/845970351217499797/C037E8872756B56162B20639E94FF3C21A5D06CA/").Respond(new ByteArrayContent(FullCycleResources.DLC_HARDCORE_Melody_PROD_SCORE317_ZONE1_LEVEL3));
                var ugcHttpClientHandlers = KernelConfig.CreateUgcHttpClientHandler(ugcHttpClientHandler);
                var ugcHttpClient = new UgcHttpClient(ugcHttpClientHandlers, telemetryClient);

                #endregion

                var account = CloudStorageAccount.DevelopmentStorageAccount;
                var blobClient = new CloudBlobClientAdapter(account.CreateCloudBlobClient());
                var directoryFactory = new CloudBlobDirectoryFactory(blobClient);

                var replaysWorker = new ReplaysWorker(247080, db, steamWebApiClient, ugcHttpClient, directoryFactory, storeClient, telemetryClient);
                var limit = 60;
                var cancellationToken = CancellationToken.None;

                // Act
                var replays = await replaysWorker.GetReplaysAsync(limit, cancellationToken).ConfigureAwait(false);
                await replaysWorker.UpdateReplaysAsync(replays, cancellationToken).ConfigureAwait(false);
                await replaysWorker.StoreReplaysAsync(replays, cancellationToken);

                // Assert
                steamWebApiClientHandler.VerifyNoOutstandingRequest();
                ugcHttpClientHandler.VerifyNoOutstandingRequest();
            }
        }
    }
}
