using Nager.CertificateManagement.Library.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.CertificateJobRepository
{
    public class MockCertificateJobRepository : ICertificateJobRepository
    {
        private readonly ConcurrentDictionary<Guid, CertificateJob> _certificateJobs = new ConcurrentDictionary<Guid, CertificateJob>();

        public Task<bool> AddCertificateJobAsync(AddCertificateJob addCertificateJob, CancellationToken cancellationToken = default)
        {
            var certificateJob = new CertificateJob
            {
                Id = Guid.NewGuid(),
                Created = DateTime.Now,
                Fqdn = addCertificateJob.Fqdn,
                Status = CertificateJobStatus.Waiting
            };

            var successful = this._certificateJobs.TryAdd(certificateJob.Id, certificateJob);
            return Task.FromResult(successful);
        }

        public Task<CertificateJob> GetCertificateJobAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this._certificateJobs.Values.SingleOrDefault(job => job.Id == id));
        }

        public Task<CertificateJob[]> GetCertificateJobsAsync(CancellationToken cancellationToken = default)
        {
            var items = this._certificateJobs.Values.ToArray();
            return Task.FromResult(items);
        }
    }
}
