using System;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    /// <summary>
    /// Represents a <see cref="Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient"/>
    /// </summary>
    internal interface ICloudBlobClient
    {
        /// <summary>
        /// Gets the base URI for the Blob service client at the primary location.
        /// </summary>
        Uri BaseUri { get; }

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