using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nager.CertificateManagement.Library.CertificateJobRepository;
using Nager.CertificateManagement.Library.Models;
using Nager.CertificateManagement.Library.ObjectStorage;
using Nager.CertificateManagement.WebApi.Services;
using Nager.PublicSuffix;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificateJobController : ControllerBase
    {
        private readonly ILogger<CertificateJobController> _logger;
        private readonly IDomainParser _domainParser;
        private readonly ICertificateJobRepository _certificateJobRepository;
        private readonly ICertificateService _certificateService;
        private readonly IObjectStorage _objectStorage;

        public CertificateJobController(
            ILogger<CertificateJobController> logger,
            IDomainParser domainParser,
            ICertificateJobRepository certificateJobRepository,
            ICertificateService certificateService,
            IObjectStorage objectStorage)
        {
            this._logger = logger;
            this._domainParser = domainParser;
            this._certificateJobRepository = certificateJobRepository;
            this._certificateService = certificateService;
            this._objectStorage = objectStorage;
        }

        [HttpGet]
        public async Task<ActionResult<CertificateJob[]>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            var items = await this._certificateJobRepository.GetAllAsync(cancellationToken);
            return StatusCode(StatusCodes.Status200OK, items.OrderByDescending(o => o.Created));
        }

        [HttpPost]
        public async Task<ActionResult> AddAsync(
            [Required] [FromBody] AddCertificateJob addCertificateJob,
            CancellationToken cancellationToken = default)
        {
            var isValid = this._domainParser.IsValidDomain(addCertificateJob.Fqdn);
            if (!isValid)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (await this._certificateJobRepository.AddAsync(addCertificateJob, cancellationToken))
            {
                _ = Task.Run(async () => await this._certificateService.CheckAsync());

                return StatusCode(StatusCodes.Status201Created);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<ActionResult> DeleteAsync(
            [Required] [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            if (await this._certificateJobRepository.DeleteAsync(id, cancellationToken))
            {
                return StatusCode(StatusCodes.Status200OK);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet]
        [Route("download/{id}")]
        public async Task<ActionResult> DownloadCertificateAsync(
            [Required] [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var item = await this._certificateJobRepository.GetAsync(id, cancellationToken);

            var fileExtensions = new[] { "key", "pem", "pfx" };

            using var memoryStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create))
            {
                foreach (var fileExtension in fileExtensions)
                {
                    var file = $"{item.Fqdn}/certificate.{fileExtension}";
                    var fileData = await this._objectStorage.GetFileAsync(file, cancellationToken);

                    var entry = zipArchive.CreateEntry(file);
                    using var writer = new BinaryWriter(entry.Open());
                    writer.Write(fileData);
                }
            }

            return File(memoryStream.ToArray(), "application/zip", $"certificate.zip");
        }
    }
}
