using Nager.CertificateManagement.Library.Models;
using Nager.CertificateManagement.Library.ObjectStorage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.CertificateJobRepository
{
    public class S3CertificateJobRepository : ICertificateJobRepository
    {
        private readonly IObjectStorage _objectStorage;
        private readonly string _prefix = "certificatejobs";

        public S3CertificateJobRepository(IObjectStorage objectStorage)
        {
            this._objectStorage = objectStorage;
        }

        private string GetObjectKey(Guid id)
        {
            return $"{this._prefix}/{id}";
        }

        public async Task<bool> AddCertificateJobAsync(AddCertificateJob addCertificateJob, CancellationToken cancellationToken = default)
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

            var json = JsonConvert.SerializeObject(certificateJob);
            var data = Encoding.UTF8.GetBytes(json);

            await this._objectStorage.AddFileAsync(this.GetObjectKey(certificateJob.Id), data, cancellationToken);

            return true;
        }

        public async Task<bool> UpdateCertificateJobStatusAsync(Guid id, CertificateJobStatus certificateJobStatus, CancellationToken cancellationToken = default)
        {
            var certificateJob = await this.GetCertificateJobAsync(id, cancellationToken);
            certificateJob.Status = certificateJobStatus;

            var json = JsonConvert.SerializeObject(certificateJob);
            var data = Encoding.UTF8.GetBytes(json);
            await this._objectStorage.AddFileAsync(this.GetObjectKey(certificateJob.Id), data, cancellationToken);

            return true;
        }

        public async Task<bool> DeleteCertificateJobAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await this._objectStorage.RemoveFileAsync(this.GetObjectKey(id), cancellationToken);
        }

        public async Task<CertificateJob> GetCertificateJobAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var data = await this._objectStorage.GetFileAsync(this.GetObjectKey(id), cancellationToken);
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<CertificateJob>(json);
        }

        public async Task<CertificateJob[]> GetCertificateJobsAsync(CancellationToken cancellationToken = default)
        {
            var keys = await this._objectStorage.GetFileKeysAsync(this._prefix, cancellationToken);

            var items = new List<CertificateJob>();
            foreach (var key in keys)
            {
                var data = await this._objectStorage.GetFileAsync(key, cancellationToken);
                var json = Encoding.UTF8.GetString(data);
                var certificateJob = JsonConvert.DeserializeObject<CertificateJob>(json);
                items.Add(certificateJob);
            }

            return items.ToArray();
        }
    }
}
