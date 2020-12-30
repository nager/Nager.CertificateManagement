using Nager.CertificateManagement.Library.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                IsAvailable = false
            };

            var successful = this._certificateJobs.TryAdd(certificateJob.Id, certificateJob);
            return Task.FromResult(successful);
        }

        public Task<CertificateJob[]> GetCertificateJobsAsync(CancellationToken cancellationToken = default)
        {
            var items = this._certificateJobs.Values.ToArray();
            return Task.FromResult(items);
        }
    }
}
