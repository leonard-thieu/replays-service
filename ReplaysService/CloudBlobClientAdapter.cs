using Microsoft.WindowsAzure.Storage.Blob;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    sealed class CloudBlobClientAdapter : ICloudBlobClient
    {
        public CloudBlobClientAdapter(CloudBlobClient cloudBlobClient)
        {
            this.cloudBlobClient = cloudBlobClient;
        }

        readonly CloudBlobClient cloudBlobClient;

        /// <summary>
        /// Returns a reference to a <see cref="ICloudBlobContainer"/> object with the specified name.
        /// </summary>
        /// <param name="containerName">A string containing the name of the container.</param>
        /// <returns>
        /// A <see cref="ICloudBlobContainer"/> object.
        /// </returns>
        public ICloudBlobContainer GetContainerReference(string containerName)
        {
            var container = cloudBlobClient.GetContainerReference(containerName);

            return new CloudBlobContainerAdapter(container);
        }
    }
}
