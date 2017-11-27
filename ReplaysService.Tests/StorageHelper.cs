using System;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    internal static class StorageHelper
    {
        public static string GetDatabaseConnectionString()
        {
            return GetConnectionString(nameof(LeaderboardsContext));
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
