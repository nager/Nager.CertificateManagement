using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.DnsProvider
{
    public interface IDnsProvider
    {
        Task<string[]> GetManagedDomainsAsync(CancellationToken cancellationToken = default);
        Task<bool> CreateAcmeChallengeRecordAsync(string fqdn, string acmeToken, CancellationToken cancellationToken = default);
        Task<bool> RemoveAcmeChallengeRecordAsync(string fqdn, CancellationToken cancellationToken = default);
    }
}
