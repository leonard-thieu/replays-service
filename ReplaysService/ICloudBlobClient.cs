namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal interface ICloudBlobClient
    {
        /// <summary>
        /// Returns a reference to a <see cref="ICloudBlobContainer"/> object with the specified name.
        /// </summary>
        /// <param name="containerName">A string containing the name of the container.</param>
        /// <returns>
        /// A <see cref="ICloudBlobContainer"/> object.
        /// </returns>
        ICloudBlobContainer GetContainerReference(string containerName);
    }
}