using System.ComponentModel.DataAnnotations;

namespace Nager.CertificateManagement.WebApi.Models
{
    public class CertificateRequestDto
    {
        [Required]
        public string Fqdn { get; set; }
    }
}
