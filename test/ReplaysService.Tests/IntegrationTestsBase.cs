using System;
using System.Configuration;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage;
using toofz.Data;
using toofz.Services.ReplaysService.Properties;
using Xunit;

namespace toofz.Services.ReplaysService.Tests
{
    [Trait("Category", "Uses SQL Server")]
    [Trait("Category", "Uses Azure Storage")]
    [Trait("Category", "Uses file system")]
    [Collection("Uses SQL Server, Azure Storage, and file system")]
    public abstract class IntegrationTestsBase : IDisposable
    {
        public static CloudStorageAccount GetCloudStorageAccount()
        {
            var connectionString = StorageHelper.GetConnectionString($"{Constants.ProjectName}AzureStorage");

            return connectionString != null ?
                CloudStorageAccount.Parse(connectionString) :
                CloudStorageAccount.DevelopmentStorageAccount;
        }

        public IntegrationTestsBase()
        {
            settings = Settings.Default;
            // Should only loop once
            foreach (SettingsProvider provider in settings.Providers)
            {
                var ssp = (ServiceSettingsProvider)provider;
                ssp.GetSettingsReader = () => File.OpenText(settingsFileName);
                ssp.GetSettingsWriter = () => File.CreateText(settingsFileName);
            }

            var options = new DbContextOptionsBuilder<NecroDancerContext>()
                .UseSqlServer(databaseConnectionString)
                .Options;

            db = new NecroDancerContext(options);
            db.Database.EnsureDeleted();
            db.Database.Migrate();
            db.EnsureSeedData();

            var storageConnectionString = GetCloudStorageAccount().ToString(exportSecrets: true);
            cloudBlobContainer = KernelConfig.CreateCloudBlobContainer(storageConnectionString, "test_crypt");
            cloudBlobContainer.DeleteIfExists();
        }

        internal readonly Settings settings;
        private readonly string settingsFileName = Path.GetTempFileName();
        protected readonly string databaseConnectionString = StorageHelper.GetDatabaseConnectionString(Constants.NecroDancerContextName);
        protected readonly NecroDancerContext db;
        internal readonly ICloudBlobContainer cloudBlobContainer;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (File.Exists(settingsFileName)) { File.Delete(settingsFileName); }
                db.Database.EnsureDeleted();
                db.Dispose();
                cloudBlobContainer.DeleteIfExists();
            }
        }
    }
}
