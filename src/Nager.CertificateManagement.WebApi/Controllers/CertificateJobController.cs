using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nager.CertificateManagement.Library.CertificateJobRepository;
using Nager.CertificateManagement.Library.Models;
using Nager.CertificateManagement.WebApi.Services;
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
        private readonly ICertificateService _certificateService;

        public CertificateJobController(
            ILogger<CertificateJobController> logger,
            ICertificateJobRepository certificateJobRepository,
            ICertificateService certificateService)
        {
            this._logger = logger;
            this._certificateJobRepository = certificateJobRepository;
            this._certificateService = certificateService;
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
            [Required][FromBody] AddCertificateJob addCertificateJob,
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
                _ = Task.Run(async () => await this._certificateService.CheckAsync());

                return StatusCode(StatusCodes.Status201Created);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
