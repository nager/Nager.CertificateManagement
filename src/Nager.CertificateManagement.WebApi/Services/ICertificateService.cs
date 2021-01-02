using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.WebApi.Services
{
    public interface ICertificateService
    {
        Task CheckAsync(CancellationToken cancellationToken = default);
    }
}
