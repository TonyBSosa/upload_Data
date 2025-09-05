using Microsoft.AspNetCore.Mvc;
using YourApp.Services;
using YourApp.Models;

namespace YourApp.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly IUploadService _uploadService;

    public UploadController(IUploadService uploadService)
    {
        _uploadService = uploadService;
    }

    [HttpPost("carga-academica")]
    public async Task<ActionResult<UploadResult>> SubirCargaAcademica(IFormFile archivo)
    {
        var resultado = await _uploadService.ProcesarArchivoAsync(archivo, "Carga Académica");
        return Ok(resultado);
    }

    [HttpPost("estudiantes")]
    public async Task<ActionResult<UploadResult>> SubirEstudiantes(IFormFile archivo)
    {
        var resultado = await _uploadService.ProcesarArchivoAsync(archivo, "Estudiantes");
        return Ok(resultado);
    }
}