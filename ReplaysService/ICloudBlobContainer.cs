using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    interface ICloudBlobContainer
    {
        /// <summary>
        /// Initiates an asynchronous operation that checks whether the container exists.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        ///  A <see cref="Task{TResult}"/> object that represents the asynchronous operation.
        /// </returns>
        Task<bool> ExistsAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Initiates an asynchronous operation that creates a container.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        ///  A <see cref="Task{TResult}"/> object that represents the asynchronous operation.
        /// </returns>
        Task CreateAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Initiates an asynchronous operation that sets permissions for the container.
        /// </summary>
        /// <param name="permissions">The permissions to apply to the container.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> object that represents the asynchronous operation.
        /// </returns>
        Task SetPermissionsAsync(BlobContainerPermissions permissions, CancellationToken cancellationToken);
        /// <summary>
        /// Gets a reference to a virtual blob directory beneath this container.
        /// </summary>
        /// <param name="relativeAddress">
        /// A string containing the name of the virtual blob directory.
        /// </param>
        /// <returns>
        /// A <see cref="ICloudBlobDirectory"/> object.
        /// </returns>
        ICloudBlobDirectory GetDirectoryReference(string relativeAddress);
    }
}