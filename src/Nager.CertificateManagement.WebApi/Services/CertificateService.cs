﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly LetsEncryptConfig _letsEncryptConfig;
        private readonly CertificateSigningInfo _certificateSigningInfo;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDomainParser _domainParser;
        private readonly ICertificateJobRepository _certificateJobRepository;
        private readonly IEnumerable<IDnsManagementProvider> _dnsManagementProviders;
        private readonly IObjectStorage _objectStorage;

        public CertificateService(
            ILogger<CertificateService> logger,
            ILoggerFactory loggerFactory,
            IOptions<LetsEncryptConfig> letsEncryptConfig,
            IOptions<CertificateSigningInfo> certificateSigningInfo,
            IDomainParser domainParser,
            ICertificateJobRepository certificateJobRepository,
            IEnumerable<IDnsManagementProvider> dnsManagementProviders,
            IObjectStorage objectStorage)
        {
            this._logger = logger;
            this._loggerFactory = loggerFactory;
            this._letsEncryptConfig = letsEncryptConfig.Value;
            this._certificateSigningInfo = certificateSigningInfo.Value;
            this._domainParser = domainParser;
            this._certificateJobRepository = certificateJobRepository;
            this._dnsManagementProviders = dnsManagementProviders;
            this._objectStorage = objectStorage;
        }

        public async Task CheckAsync(CancellationToken cancellationToken = default)
        {
            var certificateJobs = await this._certificateJobRepository.GetAllAsync(cancellationToken);
            var waitingCertificateJobs = certificateJobs.Where(o => o.Status != CertificateJobStatus.Done);

            this._logger.LogInformation("Run certificate service check");

            foreach (var certificateJob in waitingCertificateJobs)
            {
                try
                {
                    var isProcessable = false;
                    var domainInfo = this._domainParser.Parse(certificateJob.Fqdn);

                    if (!await this._objectStorage.IsReadyAsync(cancellationToken))
                    {
                        await this._certificateJobRepository.UpdateStatusAsync(certificateJob.Id, CertificateJobStatus.Failure);
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
                        await this._certificateJobRepository.UpdateStatusAsync(certificateJob.Id, CertificateJobStatus.NoDnsProvider);
                    }
                }
                catch (Exception exception)
                {
                    this._logger.LogError(exception, $"Process {certificateJob.Fqdn}");
                }
            }
        }

        private async Task<bool> ProcessJobAsync(
            CertificateJob certificateJob,
            IDnsManagementProvider dnsManagementProvider,
            CancellationToken cancellationToken = default)
        {
            var logger = this._loggerFactory.CreateLogger<CertificateProcessor>();

            var certificateProcessor = new CertificateProcessor(
                logger,
                dnsManagementProvider,
                this._objectStorage,
                this._letsEncryptConfig,
                this._certificateSigningInfo);

            await this._certificateJobRepository.UpdateStatusAsync(certificateJob.Id, CertificateJobStatus.InProgress);

            var domain = this.GetDomain(certificateJob);

            var isSuccessful = await certificateProcessor.ProcessAsync(new string[] { domain }, cancellationToken);
            if (isSuccessful)
            {
                await this._certificateJobRepository.UpdateStatusAsync(certificateJob.Id, CertificateJobStatus.Done);
            }
            else
            {
                await this._certificateJobRepository.UpdateStatusAsync(certificateJob.Id, CertificateJobStatus.Failure);
            }

            return isSuccessful;
        }

        private string GetDomain(CertificateJob certificateJob)
        {
            if (certificateJob.Wildcard)
            {
                return $"*.{certificateJob.Fqdn}";
            }

            return certificateJob.Fqdn;
        }
    }
}
