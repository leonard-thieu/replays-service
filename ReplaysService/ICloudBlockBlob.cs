using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    interface ICloudBlockBlob
    {
        /// <summary>
        /// Gets the blob's system properties.
        /// </summary>
        BlobProperties Properties { get; }
        /// <summary>
        /// Gets the blob's URI for the primary location.
        /// </summary>
        Uri Uri { get; }

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
        Task UploadFromStreamAsync(Stream source, CancellationToken cancellationToken);
    }
}
