using Microsoft.AspNetCore.Http;
using YourApp.Models;

namespace YourApp.Services;

public interface IUploadService
{
    Task<UploadResult> ProcesarArchivoAsync(IFormFile archivo, string tipo);
}