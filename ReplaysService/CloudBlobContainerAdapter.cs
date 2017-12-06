using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace toofz.Services.ReplaysService
{
    /// <summary>
    /// Wraps a <see cref="CloudBlobContainer"/> instance.
    /// </summary>
    internal sealed class CloudBlobContainerAdapter : ICloudBlobContainer
    {
        /// <summary>
        /// Initializes an instance of the <see cref="CloudBlobContainerAdapter"/> class.
        /// </summary>
        /// <param name="container">Teh <see cref="CloudBlobContainer"/> to wrap.</param>
        public CloudBlobContainerAdapter(CloudBlobContainer container)
        {
            this.container = container;
            ServiceClient = new CloudBlobClientAdapter(container.ServiceClient);
        }

        readonly CloudBlobContainer container;

        /// <summary>
        /// Gets the Blob service client for the container.
        /// </summary>
        public ICloudBlobClient ServiceClient { get; }

        /// <summary>
        /// Initiates an asynchronous operation that creates a container.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> object that represents the asynchronous operation.
        /// </returns>
        public Task CreateAsync(CancellationToken cancellationToken)
        {
            return container.CreateAsync(cancellationToken);
        }

        /// <summary>
        /// Deletes the container if it already exists.
        /// </summary>
        /// <param name="accessCondition">
        /// An <see cref="AccessCondition"/> object that represents the 
        /// condition that must be met in order for the request to proceed. If null, no condition 
        /// is used.
        /// </param>
        /// <param name="options">
        /// A <see cref="BlobRequestOptions"/> object that specifies 
        /// additional options for the request. If null, default options are applied to the 
        /// request.
        /// </param>
        /// <param name="operationContext">
        /// An <see cref="OperationContext"/> object that represents the 
        /// context for the current operation.
        /// </param>
        /// <returns>true if the container did not already exist and was created; otherwise false.</returns>
        public bool DeleteIfExists(AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null)
        {
            return container.DeleteIfExists(accessCondition, options, operationContext);
        }

        /// <summary>
        /// Initiates an asynchronous operation that checks whether the container exists.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> object that represents the asynchronous operation.
        /// </returns>
        public Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            return container.ExistsAsync(cancellationToken);
        }

        /// <summary>
        /// Gets a reference to a virtual blob directory beneath this container.
        /// </summary>
        /// <param name="relativeAddress">
        /// A string containing the name of the virtual blob directory.
        /// </param>
        /// <returns>
        /// A <see cref="ICloudBlobDirectory"/> object.
        /// </returns>
        public ICloudBlobDirectory GetDirectoryReference(string relativeAddress)
        {
            var directory = container.GetDirectoryReference(relativeAddress);

            return new CloudBlobDirectoryAdapter(directory);
        }

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
        public Task SetPermissionsAsync(BlobContainerPermissions permissions, CancellationToken cancellationToken)
        {
            return container.SetPermissionsAsync(permissions, cancellationToken);
        }
    }
}
