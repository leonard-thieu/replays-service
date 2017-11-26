using System.Threading;
using System.Threading.Tasks;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal interface ICloudBlobDirectoryFactory
    {
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
        Task<ICloudBlobDirectory> GetCloudBlobDirectoryAsync(string relativeAddress, CancellationToken cancellationToken);
    }
}