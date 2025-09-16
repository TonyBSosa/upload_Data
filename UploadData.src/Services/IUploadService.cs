using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using UploadData.Models;

namespace UploadData.Services
{
    public interface IUploadService
    {
        Task<UploadResult> ProcesarArchivoAsync(
            IFormFile archivo,
            string tipo,
            CancellationToken ct = default
        );
    }
}
