using Nager.CertificateManagement.Library.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.CertificateJobRepository
{
    public interface ICertificateJobRepository
    {
        Task<CertificateJob[]> GetCertificateJobsAsync(CancellationToken cancellationToken = default);
        Task<CertificateJob> GetCertificateJobAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> AddCertificateJobAsync(AddCertificateJob addCertificateJob, CancellationToken cancellationToken = default);
        Task<bool> UpdateCertificateJobStatusAsync(Guid id, CertificateJobStatus certificateJobStatus, CancellationToken cancellationToken = default);
        Task<bool> DeleteCertificateJobAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
