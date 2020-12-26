using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.DnsProvider
{
    public interface IDnsProvider
    {
        Task<string[]> GetManagedDomainsAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Create AcmeChallenge Record
        /// </summary>
        /// <param name="fqdn">Fully Qualified Domain Name</param>
        /// <param name="acmeToken">ACME Token</param>
        /// <returns></returns>
        Task<bool> CreateAcmeChallengeRecordAsync(string fqdn, string acmeToken, CancellationToken cancellationToken = default);
        /// <summary>
        /// Remove AcmeChallenge Record
        /// </summary>
        /// <param name="fqdn">Fully Qualified Domain Name</param>
        /// <returns></returns>
        Task<bool> RemoveAcmeChallengeRecordAsync(string fqdn, CancellationToken cancellationToken = default);
    }
}
