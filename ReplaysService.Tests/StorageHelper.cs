using System;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using Microsoft.WindowsAzure.Storage;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    internal static class StorageHelper
    {
        private const string ProjectName = "ReplaysService";

        public static string GetDatabaseConnectionString(string name)
        {
            var baseName = $"Test{ProjectName}{name}";
            var connectionString = GetConnectionString(baseName);
            if (connectionString != null) { return connectionString; }

            var connectionFactory = new LocalDbConnectionFactory("mssqllocaldb");
            using (var connection = connectionFactory.CreateConnection(baseName))
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
            return Environment.GetEnvironmentVariable($"{baseName}ConnectionString", EnvironmentVariableTarget.Machine) ??
                   ConfigurationManager.ConnectionStrings[baseName]?.ConnectionString;
        }
    }
}
