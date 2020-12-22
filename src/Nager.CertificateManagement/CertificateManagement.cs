using Certes;
using Certes.Acme;
using DnsClient;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement
{
    public class CertificateManagement
    {
        private readonly string _acmeKeyFile = "acme-account-test.pem";
        private readonly Uri _acmeDirectoryUri = WellKnownServers.LetsEncryptStagingV2;
        private readonly IDnsProvider _dnsProvider;
        private readonly CertificateSigningInfo _certificateSigningInfo;

        public CertificateManagement(
            IDnsProvider dnsProvider,
            string acmeAccountEmail,
            CertificateSigningInfo certificateSigningInfo,
            CertificateRequestMode certificateRequestMode)
        {
            this._dnsProvider = dnsProvider;
            this._certificateSigningInfo = certificateSigningInfo;

            if (certificateRequestMode == CertificateRequestMode.Production)
            {
                this._acmeDirectoryUri = WellKnownServers.LetsEncryptV2;
                this._acmeKeyFile = "acme-account-production.pem";
            }

            if (!File.Exists(this._acmeKeyFile))
            {
                var acme = new AcmeContext(this._acmeDirectoryUri);
                acme.NewAccount(acmeAccountEmail, true).GetAwaiter().GetResult();
                var accountPemKey = acme.AccountKey.ToPem();
                File.WriteAllText(this._acmeKeyFile, accountPemKey);
            }
        }

        public async Task ProcessAsync(string[] domains, CancellationToken cancellationToken = default)
        {
            var accountPemKey = await File.ReadAllTextAsync(this._acmeKeyFile, cancellationToken);
            var acme = new AcmeContext(this._acmeDirectoryUri, KeyFactory.FromPem(accountPemKey));

            var order = await acme.NewOrder(domains);

            var authorizations = await order.Authorizations();
            foreach (var authorization in authorizations)
            {
                var resource = await authorization.Resource();

                var cleanDomain = resource.Identifier.Value;
                var requestedDomain = domains.SingleOrDefault(domain => domain.Contains(cleanDomain));

                var dnsChallenge = await authorization.Dns();
                var acmeToken = acme.AccountKey.DnsTxt(dnsChallenge.Token);

                await this._dnsProvider.CreateAcmeChallengeRecordAsync(cleanDomain, acmeToken, cancellationToken);

                var dnsClient = new LookupClient(new LookupClientOptions(NameServer.GooglePublicDns, NameServer.GooglePublicDns2) { UseCache = false });
                for (var i = 0; i < 60; i++)
                {
                    var queryResponse = await dnsClient.QueryAsync($"_acme-challenge.{cleanDomain}", QueryType.TXT, cancellationToken: cancellationToken);
                    if (queryResponse.Answers.TxtRecords().Any(txtRecord => txtRecord.Text.FirstOrDefault().Equals(acmeToken, StringComparison.OrdinalIgnoreCase)))
                    {
                        break;
                    }


                    await Task.Delay(1000);
                }

                await dnsChallenge.Validate();

                var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var cert = await order.Generate(new CsrInfo
                {
                    CountryName = this._certificateSigningInfo.CountryName,
                    State = this._certificateSigningInfo.State,
                    Locality = this._certificateSigningInfo.Locality,
                    Organization = this._certificateSigningInfo.Organization,
                    OrganizationUnit = this._certificateSigningInfo.OrganizationUnit,
                    CommonName = requestedDomain,
                }, privateKey);

                if (!Directory.Exists(cleanDomain))
                {
                    Directory.CreateDirectory(cleanDomain);
                }

                await File.WriteAllTextAsync($"{cleanDomain}/certificate.pem", cert.ToPem(), cancellationToken);
                await File.WriteAllTextAsync($"{cleanDomain}/key.pem", privateKey.ToPem(), cancellationToken);

                await this._dnsProvider.RemoveAcmeChallengeRecordAsync(cleanDomain, cancellationToken);
            }
        }
    }
}
