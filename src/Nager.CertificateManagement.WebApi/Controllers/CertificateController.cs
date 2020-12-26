using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nager.CertificateManagement.Library;
using Nager.CertificateManagement.Library.DnsProvider;
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
            string fqdn,
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

            var certificateProcessor = new CertificateProcessor(this._dnsProvider, this._configuration["email"], certificateSigningInfo, CertificateRequestMode.Test);
            var successful = await certificateProcessor.ProcessAsync(new string[] { fqdn }, cancellationToken);

            if (successful)
            {
                return StatusCode(StatusCodes.Status200OK);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
