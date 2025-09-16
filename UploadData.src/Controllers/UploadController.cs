using Microsoft.AspNetCore.Mvc;
using UploadData.Services;
using UploadData.Models;

namespace UploadData.Controllers;

[ApiController]
[Route("api/upload")]
[Consumes("multipart/form-data")] // <- aplica para todas las acciones del controller
public class UploadController : ControllerBase
{
    private readonly IPlanMateriaService _planService;
    private readonly IUploadService _uploadService;

    public UploadController(IPlanMateriaService planService, IUploadService uploadService)
    {
        _planService = planService;
        _uploadService = uploadService;
    }

    [HttpPost("carga-academica")]
    [ProducesResponseType(typeof(UploadResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadResult>> SubirCargaAcademica(
        [FromForm] UploadFileRequest form, CancellationToken ct)
    {
        if (form.Archivo is null || form.Archivo.Length == 0)
            return BadRequest("Archivo vacío.");

        var resultado = await _planService.ProcesarExcelAsync(form.Archivo, ct);
        return Ok(resultado);
    }

    [HttpPost("estudiantes")]
    [ProducesResponseType(typeof(UploadResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadResult>> SubirEstudiantes(
        [FromForm] UploadFileRequest form, CancellationToken ct)
    {
        if (form.Archivo is null || form.Archivo.Length == 0)
            return BadRequest("Archivo vacío.");

        var resultado = await _uploadService.ProcesarArchivoAsync(form.Archivo, "Estudiantes", ct);
        return Ok(resultado);
    }
}
