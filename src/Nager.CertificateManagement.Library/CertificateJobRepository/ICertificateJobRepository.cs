using Nager.CertificateManagement.Library.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.CertificateJobRepository
{
    public interface ICertificateJobRepository
    {
        Task<CertificateJob[]> GetAllAsync(CancellationToken cancellationToken = default);
        Task<CertificateJob> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> AddAsync(AddCertificateJob addCertificateJob, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(Guid id, CertificateJobStatus certificateJobStatus, CancellationToken cancellationToken = default);
    }
}
