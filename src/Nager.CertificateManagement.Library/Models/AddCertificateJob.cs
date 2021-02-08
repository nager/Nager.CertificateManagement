namespace Nager.CertificateManagement.Library.Models
{
    public class AddCertificateJob
    {
        public string Fqdn { get; set; }
        public bool Wildcard { get; set; }
        public CertificateJobType JobType { get; set; }
    }
}
