 // Controllers/UploadController.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UploadData.Services;

namespace UploadData.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IUploadService _uploadService;
    public UploadController(IUploadService uploadService) => _uploadService = uploadService;

    // POST /api/Upload/excel/stg   -> carga a STG (staging/raw)
    [HttpPost("excel/stg")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadExcelToStaging([FromForm] UploadExcelDto dto, CancellationToken ct)
    {
        if (dto.File is null || dto.File.Length == 0)
            return BadRequest("Sube un archivo Excel válido.");

        var tempPath = await SaveTempAsync(dto.File, ct);

        // Procesa e inserta en STG.ofertas_raw
        var result = await _uploadService.ProcessExcelToStagingAsync(
            tempPath, dto.Sheet, dto.Semestre, ct);

        return Ok(new
        {
            target = "stg",
            file = Path.GetFileName(tempPath),
            rows_read = result.RowsRead,
            rows_inserted = result.RowsInserted,
            batch_id = result.BatchId
        });
    }

    // POST /api/Upload/excel/final -> (placeholder) carga a tabla final
    [HttpPost("excel/final")]
    [Consumes("multipart/form-data")]
    public IActionResult UploadExcelToFinal([FromForm] UploadExcelDto dto)
        => StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "Pendiente: flujo a tabla FINAL aún no implementado." });

    // Helper para guardar archivo temporalmente
    private static async Task<string> SaveTempAsync(IFormFile file, CancellationToken ct)
    {
        var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var tempPath = Path.Combine(uploadsDir, $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");
        await using var fs = System.IO.File.Create(tempPath);
        await file.CopyToAsync(fs, ct);
        return tempPath;
    }
}

public class UploadExcelDto
{
    [Required] public IFormFile File { get; set; } = default!;
    public string? Sheet { get; set; }
    public int? Semestre { get; set; }
}
