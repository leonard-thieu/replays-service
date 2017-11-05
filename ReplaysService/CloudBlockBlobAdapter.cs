using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal sealed class CloudBlockBlobAdapter : ICloudBlockBlob
    {
        public CloudBlockBlobAdapter(CloudBlockBlob cloudBlockBlob)
        {
            this.cloudBlockBlob = cloudBlockBlob;
        }

        private readonly CloudBlockBlob cloudBlockBlob;

        /// <summary>
        /// Gets the blob's system properties.
        /// </summary>
        public BlobProperties Properties => cloudBlockBlob.Properties;
        /// <summary>
        /// Gets the blob's URI for the primary location.
        /// </summary>
        public Uri Uri { get => cloudBlockBlob.Uri; }

        /// <summary>
        /// Initiates an asynchronous operation to upload a stream to a block blob. If the 
        /// blob already exists, it will be overwritten.
        /// </summary>
        /// <param name="source">A <see cref="Stream"/> object providing the blob content.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> object that represents the asynchronous operation.
        /// </returns>
        public Task UploadFromStreamAsync(Stream source, CancellationToken cancellationToken)
        {
            return cloudBlockBlob.UploadFromStreamAsync(source, cancellationToken);
        }
    }
}
