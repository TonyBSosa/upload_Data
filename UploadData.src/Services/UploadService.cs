using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using UploadData.Models;

namespace UploadData.Services
{
     public class UploadService : IUploadService
    {
        public Task<UploadResult> ProcesarArchivoAsync(
            IFormFile archivo,
            string tipo,
            CancellationToken ct = default)
        {
            var res = new UploadResult
            {
                Rows       = (archivo != null && archivo.Length > 0) ? 1 : 0,
                Inserted   = 0,
                Updated    = 0,
                TipoArchivo = tipo,
                TamañoKb    = (archivo != null) ? archivo.Length / 1024 : 0,
                Mensaje     = "Archivo recibido"
            };

            return Task.FromResult(res);
        }
    }
}
