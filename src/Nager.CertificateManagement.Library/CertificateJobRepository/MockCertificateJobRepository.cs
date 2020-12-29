using Nager.CertificateManagement.Library.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.CertificateJobRepository
{
    public class MockCertificateJobRepository : ICertificateJobRepository
    {
        public Task<CertificateJob[]> GetCertificateJobsAsync(CancellationToken cancellationToken = default)
        {
            var items = new List<CertificateJob>()
            {
                new CertificateJob
                {
                    Id = Guid.Parse("2C61EF52-7D39-4D05-BED7-126580EB0615"),
                    Fqdn = "dev.test.nager.at",
                    Created = new DateTime(2020, 12, 29)
                }
            };

            return Task.FromResult(items.ToArray());
        }
    }
}
