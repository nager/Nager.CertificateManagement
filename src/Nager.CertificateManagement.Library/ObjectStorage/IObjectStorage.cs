using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.ObjectStorage
{
    public interface IObjectStorage
    {
        Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
        Task<string[]> GetFileKeysAsync(string prefix, CancellationToken cancellationToken = default);
        Task<byte[]> GetFileAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> AddFileAsync(string key, byte[] data, CancellationToken cancellationToken = default);
        Task<bool> RemoveFileAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> FileExistsAsync(string key, CancellationToken cancellationToken = default);
        
    }
}
