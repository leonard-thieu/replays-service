﻿using System;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Microsoft.ApplicationInsights;
using Moq;
using Ninject.Extensions.NamedScope;
using toofz.Data;
using toofz.Steam.WebApi;
using toofz.Steam.Workshop;
using Xunit;

namespace toofz.Services.ReplaysService.Tests
{
    public class WorkerRoleTests
    {
        public class IntegrationTests : IntegrationTestsBase
        {
            private readonly Mock<ILog> mockLog = new Mock<ILog>();

            [DisplayFact]
            public async Task ExecutesUpdateCycle()
            {
                // Arrange
                for (int i = 1; i <= 10; i++)
                {
                    db.Replays.Add(new Replay { ReplayId = i });
                }
                db.SaveChanges();

                settings.UpdateInterval = TimeSpan.Zero;
                var telemetryClient = new TelemetryClient();
                var runOnce = true;

                var kernel = KernelConfig.CreateKernel();

                kernel.Rebind<string>()
                      .ToConstant(databaseConnectionString)
                      .WhenInjectedInto(typeof(NecroDancerContextOptionsBuilder), typeof(LeaderboardsStoreClient));

                kernel.Rebind<ISteamWebApiClient>()
                      .To<FakeSteamWebApiClient>()
                      .InParentScope();

                kernel.Rebind<IUgcHttpClient>()
                      .To<FakeUgcHttpClient>()
                      .InParentScope();

                kernel.Rebind<ICloudBlobContainer>()
                      .ToConstant(cloudBlobContainer)
                      .InParentScope();

                var log = mockLog.Object;

                // Act
                using (var worker = new WorkerRole(settings, telemetryClient, runOnce, kernel, log))
                {
                    worker.Start();
                    await worker.Completion;
                }

                // Assert
                Assert.NotEqual(0, db.Replays.Count());
                Assert.False(db.Replays.Any(r => r.Uri == null));
            }
        }
    }
}
