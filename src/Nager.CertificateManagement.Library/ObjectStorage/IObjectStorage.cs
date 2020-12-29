using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.ObjectStorage
{
    public interface IObjectStorage
    {
        Task<byte[]> GetFileAsync(string key, CancellationToken cancellationToken = default);
        Task AddFileAsync(string key, byte[] data, CancellationToken cancellationToken = default);
        Task<bool> FileExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}
