using System.Threading;
using System.Threading.Tasks;

namespace toofz.NecroDancer.Leaderboards.ReplaysService
{
    internal interface ICloudBlobDirectoryFactory
    {
        Task<ICloudBlobDirectory> GetCloudBlobDirectoryAsync(string containerName, string relativeAddress, CancellationToken cancellationToken);
    }
}