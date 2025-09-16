using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using UploadData.Models;

namespace UploadData.Services;

public interface IPlanMateriaService
{
    Task<UploadResult> ProcesarExcelAsync(IFormFile file, CancellationToken ct);
}
