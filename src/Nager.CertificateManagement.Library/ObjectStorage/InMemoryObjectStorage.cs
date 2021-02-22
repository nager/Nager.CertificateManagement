using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.ObjectStorage
{
    public class InMemoryObjectStorage : IObjectStorage
    {
        private readonly ConcurrentDictionary<string, byte[]> _cache = new ConcurrentDictionary<string, byte[]>();

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> AddFileAsync(string key, byte[] data, CancellationToken cancellationToken = default)
        {
            _ = this._cache.AddOrUpdate(key, data, (key, oldValue) => data);

            return Task.FromResult(true);
        }

        public Task<bool> FileExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            var isSuccessful = this._cache.TryGetValue(key, out var _);
            return Task.FromResult(isSuccessful);
        }

        public Task<byte[]> GetFileAsync(string key, CancellationToken cancellationToken = default)
        {
            this._cache.TryGetValue(key, out var data);
            return Task.FromResult(data);
        }

        public Task<string[]> GetFileKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var keys = this._cache.Keys.Where(o => o.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray();
            return Task.FromResult(keys);
        }

        public Task<bool> RemoveFileAsync(string key, CancellationToken cancellationToken = default)
        {
            var isSuccessful = this._cache.TryRemove(key, out var _);
            return Task.FromResult(isSuccessful);
        }
    }
}
