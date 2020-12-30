using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nager.CertificateManagement.Library.CertificateJobRepository;
using Nager.CertificateManagement.Library.Models;
using Nager.PublicSuffix;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificateJobController : ControllerBase
    {
        private readonly ILogger<CertificateJobController> _logger;
        private readonly ICertificateJobRepository _certificateJobRepository;

        public CertificateJobController(
            ILogger<CertificateJobController> logger,
            ICertificateJobRepository certificateJobRepository)
        {
            this._logger = logger;
            this._certificateJobRepository = certificateJobRepository;
        }

        [HttpGet]
        public async Task<ActionResult<CertificateJob[]>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            var items = await this._certificateJobRepository.GetCertificateJobsAsync(cancellationToken);
            return StatusCode(StatusCodes.Status200OK, items);
        }

        [HttpPost]
        public async Task<ActionResult> AddAsync(
            [Required] [FromBody] AddCertificateJob addCertificateJob,
            CancellationToken cancellationToken = default)
        {
            var domainParser = new DomainParser(new WebTldRuleProvider());
            var isValid = domainParser.IsValidDomain(addCertificateJob.Fqdn);
            if (!isValid)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }


            if (await this._certificateJobRepository.AddCertificateJobAsync(addCertificateJob, cancellationToken))
            {
                return StatusCode(StatusCodes.Status201Created);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
