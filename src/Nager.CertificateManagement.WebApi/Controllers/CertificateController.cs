using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nager.CertificateManagement.Library;
using Nager.CertificateManagement.Library.DnsProvider;
using Nager.CertificateManagement.WebApi.Models;
using Nager.PublicSuffix;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificateController : ControllerBase
    {
        private readonly ILogger<CertificateController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDnsProvider _dnsProvider;

        public CertificateController(
            ILogger<CertificateController> logger,
            IConfiguration configuration,
            IDnsProvider dnsProvider)
        {
            this._logger = logger;
            this._configuration = configuration;
            this._dnsProvider = dnsProvider;
        }

        [HttpPost]
        public async Task<ActionResult> RequestAsync(
            [FromBody] CertificateRequestDto certificateRequest,
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

            var domainParser = new DomainParser(new WebTldRuleProvider());

            var allowedDomains = await this._dnsProvider.GetManagedDomainsAsync(cancellationToken);
            var domainInfo = domainParser.Get(certificateRequest.Fqdn);
            if (!allowedDomains.Contains(domainInfo.RegistrableDomain))
            {
                this.ModelState.AddModelError(nameof(certificateRequest.Fqdn), "Domain is not supported by provider");
                return StatusCode(StatusCodes.Status422UnprocessableEntity, this.ModelState);
            }

            var certificateProcessor = new CertificateProcessor(this._dnsProvider, this._configuration["email"], certificateSigningInfo, CertificateRequestMode.Test);

            var successful = await certificateProcessor.ProcessAsync(new string[] { certificateRequest.Fqdn }, cancellationToken);
            if (successful)
            {
                return StatusCode(StatusCodes.Status200OK);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
