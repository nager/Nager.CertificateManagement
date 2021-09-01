using CloudFlare.Client;
using CloudFlare.Client.Api.Parameters;
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

        private string GetAcmeDomain(DomainInfo domainInfo)
        {
            var acmeName = "_acme-challenge";

            if (string.IsNullOrEmpty(domainInfo.SubDomain))
            {
                return acmeName;
            }

            return $"{acmeName}.{domainInfo.SubDomain}";
        }

        public async Task<string[]> GetManagedDomainsAsync(CancellationToken cancellationToken = default)
        {
            var response = await this._dnsClient.Zones.GetAsync(cancellationToken: cancellationToken);
            return response.Result.Select(zone => zone.Name).ToArray();
        }

        ///<inheritdoc/>
        public async Task<bool> CreateAcmeChallengeRecordAsync(string fqdn, string acmeToken, CancellationToken cancellationToken = default)
        {
            var domainInfo = this._domainParser.Parse(fqdn);

            var zoneFiler = new ZoneFilter
            {
                Name = domainInfo.RegistrableDomain
            };

            var zoneResponse = await this._dnsClient.Zones.GetAsync(zoneFiler, cancellationToken: cancellationToken);
            var zone = zoneResponse.Result.SingleOrDefault(zone => zone.Name.Equals(domainInfo.RegistrableDomain, StringComparison.OrdinalIgnoreCase));
            if (zone == null)
            {
                return false;
            }

            var acmeDomain = this.GetAcmeDomain(domainInfo);

            await this._dnsClient.Zones.DnsRecords.AddAsync(zone.Id, DnsRecordType.Txt, acmeDomain, acmeToken, cancellationToken: cancellationToken);

            return true;
        }

        ///<inheritdoc/>
        public async Task<bool> RemoveAcmeChallengeRecordAsync(string fqdn, CancellationToken cancellationToken = default)
        {
            var domainInfo = this._domainParser.Parse(fqdn);

            var zoneFiler = new ZoneFilter
            {
                Name = domainInfo.RegistrableDomain
            };

            var zoneResponse = await this._dnsClient.Zones.GetAsync(zoneFiler, cancellationToken: cancellationToken);
            var zone = zoneResponse.Result.SingleOrDefault(zone => zone.Name.Equals(domainInfo.RegistrableDomain, StringComparison.OrdinalIgnoreCase));
            if (zone == null)
            {
                return false;
            }

            var acmeDomain = this.GetAcmeDomain(domainInfo);

            var recordResponse = await this._dnsClient.Zones.DnsRecords.GetAsync(zone.Id, cancellationToken: cancellationToken);
            var records = recordResponse.Result.Where(record => 
                record.Name.StartsWith(acmeDomain, StringComparison.OrdinalIgnoreCase) && 
                record.Type == DnsRecordType.Txt);

            if (records == null)
            {
                return false;
            }

            foreach (var record in records)
            {
                await this._dnsClient.Zones.DnsRecords.DeleteAsync(zone.Id, record.Id, cancellationToken);
            }

            return true;
        }
    }
}
