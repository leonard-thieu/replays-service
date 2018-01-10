using System;
using Microsoft.WindowsAzure.Storage.Blob;

namespace toofz.Services.ReplaysService
{
    /// <summary>
    /// Wraps a <see cref="CloudBlobClient"/>.
    /// </summary>
    internal sealed class CloudBlobClientAdapter : ICloudBlobClient
    {
        /// <summary>
        /// Initializes an instance of the <see cref="CloudBlobClientAdapter"/> class.
        /// </summary>
        /// <param name="cloudBlobClient">The <see cref="CloudBlobClient"/> to wrap.</param>
        public CloudBlobClientAdapter(CloudBlobClient cloudBlobClient)
        {
            this.cloudBlobClient = cloudBlobClient;
        }

        private readonly CloudBlobClient cloudBlobClient;

        /// <summary>
        /// Gets the base URI for the Blob service client at the primary location.
        /// </summary>
        public Uri BaseUri => cloudBlobClient.BaseUri;

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
