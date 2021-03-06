﻿using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace toofz.Services.ReplaysService
{
    internal sealed class CloudBlobDirectoryFactory : ICloudBlobDirectoryFactory
    {
        /// <summary>
        /// Initializes an instance of the <see cref="CloudBlobDirectoryFactory"/> class.
        /// </summary>
        /// <param name="container">
        /// The <see cref="ICloudBlobContainer"/> to get virtual blob directories from.
        /// </param>
        public CloudBlobDirectoryFactory(ICloudBlobContainer container)
        {
            this.container = container;
        }

        private readonly ICloudBlobContainer container;

        /// <summary>
        /// Gets a reference to a virtual blob directory beneath this container, creating the container if it does not exist.
        /// </summary>
        /// <param name="relativeAddress">A string containing the name of the virtual blob directory.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> object that represents the asynchronous operation.
        /// </returns>
        public async Task<ICloudBlobDirectory> GetCloudBlobDirectoryAsync(string relativeAddress, CancellationToken cancellationToken)
        {
            try
            {
                var containerExists = await container.ExistsAsync(cancellationToken).ConfigureAwait(false);
                if (!containerExists)
                {
                    await container.CreateAsync(cancellationToken).ConfigureAwait(false);
                }
                var permissions = new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob };
                await container.SetPermissionsAsync(permissions, cancellationToken).ConfigureAwait(false);

                return container.GetDirectoryReference(relativeAddress);
            }
            catch (StorageException ex) when (IsDevelopmentStorageConnectFailure(ex))
            {
                throw new WebException(
                    "Could not connect to development storage. Ensure Azurite or Azure Storage Emulator is running.",
                    ex,
                    WebExceptionStatus.ConnectFailure,
                    response: null);
            }

            bool IsDevelopmentStorageConnectFailure(StorageException ex)
            {
                if (ex.InnerException is WebException webEx)
                {
                    if (webEx.InnerException is SocketException socketEx)
                    {
                        return (socketEx.SocketErrorCode == SocketError.ConnectionRefused) &&
                               (container.ServiceClient.BaseUri == CloudStorageAccount.DevelopmentStorageAccount.BlobEndpoint);
                    }
                }

                return false;
            }
        }
    }
}
