namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    interface ICloudBlobDirectory
    {
        /// <summary>
        /// Gets a reference to a block blob in this virtual directory.
        /// </summary>
        /// <param name="blobName">A string containing the name of the blob.</param>
        /// <returns>
        /// An <see cref="ICloudBlockBlob"/> object.
        /// </returns>
        ICloudBlockBlob GetBlockBlobReference(string blobName);
    }
}
