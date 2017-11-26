using Microsoft.WindowsAzure.Storage.Blob;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    /// <summary>
    /// Wraps a <see cref="CloudBlobDirectory"/>.
    /// </summary>
    internal sealed class CloudBlobDirectoryAdapter : ICloudBlobDirectory
    {
        /// <summary>
        /// Initializes an instance of the <see cref="CloudBlobDirectoryAdapter"/> class.
        /// </summary>
        /// <param name="directory">The <see cref="CloudBlobDirectory"/> to wrap.</param>
        public CloudBlobDirectoryAdapter(CloudBlobDirectory directory)
        {
            this.directory = directory;
        }

        private readonly CloudBlobDirectory directory;

        /// <summary>
        /// Gets a reference to a block blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <returns>
        /// An <see cref="ICloudBlockBlob"/> object.
        /// </returns>
        public ICloudBlockBlob GetBlockBlobReference(string blobName)
        {
            return new CloudBlockBlobAdapter(directory.GetBlockBlobReference(blobName));
        }
    }
}
