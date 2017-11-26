using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal sealed class CloudBlobDirectoryFactory : ICloudBlobDirectoryFactory
    {
        public CloudBlobDirectoryFactory(ICloudBlobClient blobClient)
        {
            this.blobClient = blobClient;
        }

        private readonly ICloudBlobClient blobClient;

        public async Task<ICloudBlobDirectory> GetCloudBlobDirectoryAsync(
            string containerName,
            string relativeAddress,
            CancellationToken cancellationToken)
        {
            var container = blobClient.GetContainerReference(containerName);
            var containerExists = await container.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!containerExists)
            {
                await container.CreateAsync(cancellationToken).ConfigureAwait(false);
            }
            var permissions = new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob };
            await container.SetPermissionsAsync(permissions, cancellationToken).ConfigureAwait(false);

            return container.GetDirectoryReference(relativeAddress);
        }
    }
}
