using Xunit;

namespace toofz.NecroDancer.Leaderboards.ReplaysService.Tests
{
    [CollectionDefinition(Name)]
    public sealed class AzureStorageCollection : ICollectionFixture<AzureStorageFixture>
    {
        public const string Name = "Azure Storage collection";
    }
}
