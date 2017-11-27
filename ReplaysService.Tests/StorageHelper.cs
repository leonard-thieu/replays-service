using System;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using Microsoft.WindowsAzure.Storage;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    internal static class StorageHelper
    {
        public static string GetDatabaseConnectionString()
        {
            var connectionString = GetConnectionString(nameof(LeaderboardsContext));
            if (connectionString != null) { return connectionString; }

            var connectionFactory = new LocalDbConnectionFactory("mssqllocaldb");
            using (var connection = connectionFactory.CreateConnection("TestReplaysService"))
            {
                return connection.ConnectionString;
            }
        }

        public static CloudStorageAccount GetCloudStorageAccount()
        {
            var connectionString = GetConnectionString("AzureStorage");

            return connectionString != null ?
                CloudStorageAccount.Parse(connectionString) :
                CloudStorageAccount.DevelopmentStorageAccount;
        }

        private static string GetConnectionString(string baseName)
        {
            return Environment.GetEnvironmentVariable($"{baseName}TestConnectionString", EnvironmentVariableTarget.Machine) ??
                ConfigurationManager.ConnectionStrings[baseName]?.ConnectionString;
        }
    }
}
