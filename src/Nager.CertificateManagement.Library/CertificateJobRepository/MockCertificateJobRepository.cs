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
                Wildcard = addCertificateJob.Wildcard,
                JobType = addCertificateJob.JobType,
                Status = CertificateJobStatus.Waiting
            };

            var successful = this._certificateJobs.TryAdd(certificateJob.Id, certificateJob);
            return Task.FromResult(successful);
        }

        public Task<bool> UpdateCertificateJobStatusAsync(Guid id, CertificateJobStatus certificateJobStatus, CancellationToken cancellationToken = default)
        {
            this._certificateJobs.TryGetValue(id, out var certificateJob);
            certificateJob.Status = certificateJobStatus;

            return Task.FromResult(true);
        }

        public Task<bool> DeleteCertificateJobAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var successful = this._certificateJobs.TryRemove(id, out _);
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
