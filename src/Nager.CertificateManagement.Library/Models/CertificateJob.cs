using System;

namespace Nager.CertificateManagement.Library.Models
{
    public class CertificateJob
    {
        public Guid Id { get; set; }
        public string Fqdn { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public CertificateJobStatus Status { get; set; }
    }
}
