using Nager.CertificateManagement.Library.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.CertificateJobRepository
{
    public interface ICertificateJobRepository
    {
        Task<CertificateJob[]> GetCertificateJobsAsync(CancellationToken cancellationToken = default);
    }
}
