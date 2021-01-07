using Certes;
using Certes.Acme;
using DnsClient;
using Microsoft.Extensions.Logging;
using Nager.CertificateManagement.Library.DnsManagementProvider;
using Nager.CertificateManagement.Library.ObjectStorage;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library
{
    public class CertificateProcessor
    {
        private readonly string _acmeKeyFile = "acme-account-test.pem";
        private readonly Uri _acmeDirectoryUri = WellKnownServers.LetsEncryptStagingV2;

        private readonly ILogger<CertificateProcessor> _logger;
        private readonly IDnsManagementProvider _dnsManagementProvider;
        private readonly IObjectStorage _objectStorage;
        private readonly CertificateSigningInfo _certificateSigningInfo;

        public CertificateProcessor(
            ILogger<CertificateProcessor> logger,
            IDnsManagementProvider dnsManagementProvider,
            IObjectStorage objectStorage,
            string acmeAccountEmail,
            CertificateSigningInfo certificateSigningInfo,
            CertificateRequestMode certificateRequestMode)
        {
            this._logger = logger;
            this._dnsManagementProvider = dnsManagementProvider;
            this._objectStorage = objectStorage;

            this._certificateSigningInfo = certificateSigningInfo;

            if (certificateRequestMode == CertificateRequestMode.Production)
            {
                this._acmeDirectoryUri = WellKnownServers.LetsEncryptV2;
                this._acmeKeyFile = "acme-account-production.pem";
            }

            if (!this._objectStorage.FileExistsAsync($"config/{this._acmeKeyFile}").GetAwaiter().GetResult())
            {
                var acme = new AcmeContext(this._acmeDirectoryUri);
                acme.NewAccount(acmeAccountEmail, true).GetAwaiter().GetResult();
                var accountPemKey = acme.AccountKey.ToPem();

                this._objectStorage.AddFileAsync($"config/{this._acmeKeyFile}", Encoding.UTF8.GetBytes(accountPemKey)).GetAwaiter().GetResult();
            }
        }

        public async Task<bool> ProcessAsync(string[] domains, CancellationToken cancellationToken = default)
        {
            var accountPemKeyData = await this._objectStorage.GetFileAsync($"config/{this._acmeKeyFile}", cancellationToken);
            var accountPemKey = Encoding.UTF8.GetString(accountPemKeyData);

            var acme = new AcmeContext(this._acmeDirectoryUri, KeyFactory.FromPem(accountPemKey));

            this._logger.LogInformation($"Create order for {string.Join(',', domains)}");
            var order = await acme.NewOrder(domains);

            var authorizations = await order.Authorizations();
            foreach (var authorization in authorizations)
            {
                var resource = await authorization.Resource();

                var cleanDomain = resource.Identifier.Value;
                var requestedDomain = domains.SingleOrDefault(domain => domain.Contains(cleanDomain));

                var dnsChallenge = await authorization.Dns();
                var acmeToken = acme.AccountKey.DnsTxt(dnsChallenge.Token);

                if (!await this._dnsManagementProvider.CreateAcmeChallengeRecordAsync(cleanDomain, acmeToken, cancellationToken))
                {
                    this._logger.LogError($"Cannot create acme challenge for {cleanDomain}");
                    return false;
                }

                var lookupClientOptions = new LookupClientOptions(NameServer.GooglePublicDns, NameServer.GooglePublicDns2)
                {
                    UseCache = false,
                    MaximumCacheTimeout = TimeSpan.Zero,
                    Retries = 0
                };
                var dnsClient = new LookupClient(lookupClientOptions);

                var maxRetries = 600;
                for (var i = 0; i < maxRetries; i++)
                {
                    this._logger.LogInformation($"Check google dns for {cleanDomain} {i}/{maxRetries}");

                    var queryResponse = await dnsClient.QueryAsync($"_acme-challenge.{cleanDomain}", QueryType.TXT, cancellationToken: cancellationToken);
                    if (queryResponse.Answers.TxtRecords().Any(txtRecord => txtRecord.Text.FirstOrDefault().Equals(acmeToken, StringComparison.OrdinalIgnoreCase)))
                    {
                        break;
                    }

                    await Task.Delay(1000);
                }

                await Task.Delay(2000);

                try
                {
                    await dnsChallenge.Validate();
                }
                catch (Exception exception)
                {
                    this._logger.LogError(exception, "Validate failure");
                    return false;
                }

                this._logger.LogInformation($"Generate certifiacte {cleanDomain}");

                var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);

                var cert = await order.Generate(new CsrInfo
                {
                    CountryName = this._certificateSigningInfo.CountryName,
                    State = this._certificateSigningInfo.State,
                    Locality = this._certificateSigningInfo.Locality,
                    Organization = this._certificateSigningInfo.Organization,
                    OrganizationUnit = this._certificateSigningInfo.OrganizationUnit,
                    CommonName = requestedDomain
                }, privateKey);

                var pfxBuilder = cert.ToPfx(privateKey);
                var pfxData = pfxBuilder.Build(cleanDomain, string.Empty);

                var keyData = Encoding.UTF8.GetBytes(privateKey.ToPem());
                var certificateData = Encoding.UTF8.GetBytes(cert.ToPem());

                this._logger.LogInformation($"Upload certificate {cleanDomain}");
                await this._objectStorage.AddFileAsync($"{cleanDomain}/certificate.key", keyData, cancellationToken);
                await this._objectStorage.AddFileAsync($"{cleanDomain}/certificate.pem", certificateData, cancellationToken);
                await this._objectStorage.AddFileAsync($"{cleanDomain}/certificate.pfx", pfxData, cancellationToken);

                this._logger.LogInformation($"Cleanup acme challenge for {cleanDomain}");

                await this._dnsManagementProvider.RemoveAcmeChallengeRecordAsync(cleanDomain, cancellationToken);
            }

            return true;
        }
    }
}
