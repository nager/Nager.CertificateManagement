using Nager.HetznerDns;
using Nager.HetznerDns.Models;
using Nager.PublicSuffix;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.DnsManagementProvider
{
    public class HetznerDnsManagementProvider : IDnsManagementProvider
    {
        private readonly HetznerDnsClient _dnsClient;
        private readonly DomainParser _domainParser;

        public HetznerDnsManagementProvider(string apiKey)
        {
            this._domainParser = new DomainParser(new WebTldRuleProvider());
            this._dnsClient = new HetznerDnsClient(apiKey);
        }

        public async Task<string[]> GetManagedDomainsAsync(CancellationToken cancellationToken = default)
        {
            var response = await this._dnsClient.GetZonesAsync(cancellationToken);
            return response.Zones.Select(zone => zone.Name).ToArray();
        }

        ///<inheritdoc/>
        public async Task<bool> CreateAcmeChallengeRecordAsync(string fqdn, string acmeToken, CancellationToken cancellationToken = default)
        {
            var domainInfo = this._domainParser.Parse(fqdn);

            var zoneResponse = await this._dnsClient.GetZonesAsync(cancellationToken);
            var zone = zoneResponse.Zones.SingleOrDefault(zone => zone.Name.Equals(domainInfo.RegistrableDomain, StringComparison.OrdinalIgnoreCase));
            if (zone == null)
            {
                return false;
            }

            var createRecord = new CreateRecord
            {
                ZoneId = zone.Id,
                Type = DnsRecordType.TXT,
                Name = $"_acme-challenge.{domainInfo.SubDomain}",
                Value = acmeToken,
                Ttl = 0
            };

            await this._dnsClient.CreateRecordAsync(createRecord, cancellationToken);

            return true;
        }

        ///<inheritdoc/>
        public async Task<bool> RemoveAcmeChallengeRecordAsync(string fqdn, CancellationToken cancellationToken = default)
        {
            var domainInfo = this._domainParser.Parse(fqdn);

            var zoneResponse = await this._dnsClient.GetZonesAsync(cancellationToken);
            var zone = zoneResponse.Zones.SingleOrDefault(zone => zone.Name.Equals(domainInfo.RegistrableDomain, StringComparison.OrdinalIgnoreCase));
            if (zone == null)
            {
                return false;
            }

            var recordResponse = await this._dnsClient.GetRecordsAsync(zone.Id, cancellationToken);
            var records = recordResponse.Records.Where(record => 
                record.Name.Equals($"_acme-challenge.{domainInfo.SubDomain}", StringComparison.OrdinalIgnoreCase) && 
                record.Type == DnsRecordType.TXT);

            if (records == null)
            {
                return false;
            }

            foreach (var record in records)
            {
                await this._dnsClient.DeleteRecordAsync(record.Id, cancellationToken);
            }

            return true;
        }
    }
}
