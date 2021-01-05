using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nager.CertificateManagement.Library;
using Nager.CertificateManagement.Library.CertificateJobRepository;
using Nager.CertificateManagement.Library.DnsManagementProvider;
using Nager.CertificateManagement.Library.Models;
using Nager.CertificateManagement.Library.ObjectStorage;
using Nager.PublicSuffix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.WebApi.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly ILogger<CertificateService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDomainParser _domainParser;
        private readonly ICertificateJobRepository _certificateJobRepository;
        private readonly IEnumerable<IDnsManagementProvider> _dnsManagementProviders;
        private readonly IObjectStorage _objectStorage;

        public CertificateService(
            ILogger<CertificateService> logger,
            IConfiguration configuration,
            IDomainParser domainParser,
            ICertificateJobRepository certificateJobRepository,
            IEnumerable<IDnsManagementProvider> dnsManagementProviders,
            IObjectStorage objectStorage)
        {
            this._logger = logger;
            this._configuration = configuration;
            this._domainParser = domainParser;
            this._certificateJobRepository = certificateJobRepository;
            this._dnsManagementProviders = dnsManagementProviders;
            this._objectStorage = objectStorage;
        }

        public async Task CheckAsync(CancellationToken cancellationToken = default)
        {
            var certificateJobs = await this._certificateJobRepository.GetCertificateJobsAsync(cancellationToken);
            var waitingCertificateJobs = certificateJobs.Where(o => o.Status != CertificateJobStatus.Done);

            foreach (var certificateJob in waitingCertificateJobs)
            {
                var isProcessable = false;
                var domainInfo = this._domainParser.Parse(certificateJob.Fqdn);

                if (!await this._objectStorage.IsReadyAsync(cancellationToken))
                {
                    certificateJob.Status = CertificateJobStatus.Failure;
                    continue;
                }

                foreach (var dnsManagementProvider in this._dnsManagementProviders)
                {
                    var allowedDomains = await dnsManagementProvider.GetManagedDomainsAsync(cancellationToken);
                    if (!allowedDomains.Contains(domainInfo.RegistrableDomain))
                    {
                        continue;
                    }

                    isProcessable = true;
                    await this.ProcessJobAsync(certificateJob, dnsManagementProvider, cancellationToken);
                }

                if (!isProcessable)
                {
                    certificateJob.Status = CertificateJobStatus.NoDnsProvider;
                }
            }
        }

        private async Task<bool> ProcessJobAsync(
            CertificateJob certificateJob,
            IDnsManagementProvider dnsManagementProvider,
            CancellationToken cancellationToken = default)
        {
            var certificateSigningInfo = new CertificateSigningInfo
            {
                CountryName = "AT",
                State = "Vorarlberg",
                Locality = "Dornbirn",
                Organization = "Company",
                OrganizationUnit = "Dev"
            };

            var certificateProcessor = new CertificateProcessor(
                dnsManagementProvider,
                this._objectStorage,
                this._configuration["email"],
                certificateSigningInfo,
                CertificateRequestMode.Test);

            certificateJob.Status = CertificateJobStatus.InProgress;

            var isSuccessful = await certificateProcessor.ProcessAsync(new string[] { certificateJob.Fqdn }, cancellationToken);
            if (isSuccessful)
            {
                certificateJob.Status = CertificateJobStatus.Done;
            }

            return isSuccessful;
        }
    }
}
