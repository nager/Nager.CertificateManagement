using CloudFlare.Client;
using CloudFlare.Client.Enumerators;
using Nager.PublicSuffix;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.DnsManagementProvider
{
    public class CloudFlareDnsManagementProvider : IDnsManagementProvider
    {
        private readonly CloudFlareClient _dnsClient;
        private readonly DomainParser _domainParser;

        public CloudFlareDnsManagementProvider(string apiKey)
        {
            this._domainParser = new DomainParser(new WebTldRuleProvider());
            this._dnsClient = new CloudFlareClient(apiKey);
        }

        public async Task<string[]> GetManagedDomainsAsync(CancellationToken cancellationToken = default)
        {
            var response = await this._dnsClient.GetZonesAsync(cancellationToken);
            return response.Result.Select(zone => zone.Name).ToArray();
        }

        ///<inheritdoc/>
        public async Task<bool> CreateAcmeChallengeRecordAsync(string fqdn, string acmeToken, CancellationToken cancellationToken = default)
        {
            var domainInfo = this._domainParser.Parse(fqdn);

            var zoneResponse = await this._dnsClient.GetZonesAsync(domainInfo.RegistrableDomain, cancellationToken);
            var zone = zoneResponse.Result.SingleOrDefault(zone => zone.Name.Equals(domainInfo.RegistrableDomain, StringComparison.OrdinalIgnoreCase));
            if (zone == null)
            {
                return false;
            }

            var name = $"_acme-challenge.{domainInfo.SubDomain}";

            await this._dnsClient.CreateDnsRecordAsync(zone.Id, DnsRecordType.Txt, name, acmeToken, cancellationToken);

            return true;
        }

        ///<inheritdoc/>
        public async Task<bool> RemoveAcmeChallengeRecordAsync(string fqdn, CancellationToken cancellationToken = default)
        {
            var domainInfo = this._domainParser.Parse(fqdn);

            var zoneResponse = await this._dnsClient.GetZonesAsync(domainInfo.RegistrableDomain, cancellationToken);
            var zone = zoneResponse.Result.SingleOrDefault(zone => zone.Name.Equals(domainInfo.RegistrableDomain, StringComparison.OrdinalIgnoreCase));
            if (zone == null)
            {
                return false;
            }

            var recordResponse = await this._dnsClient.GetDnsRecordsAsync(zone.Id, cancellationToken);
            var records = recordResponse.Result.Where(record => 
                record.Name.StartsWith($"_acme-challenge.{domainInfo.SubDomain}", StringComparison.OrdinalIgnoreCase) && 
                record.Type == DnsRecordType.Txt);

            if (records == null)
            {
                return false;
            }

            foreach (var record in records)
            {
                await this._dnsClient.DeleteDnsRecordAsync(zone.Id, record.Id, cancellationToken);
            }

            return true;
        }
    }
}
