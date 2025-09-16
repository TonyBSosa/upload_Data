using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using UploadData.Models;
using UploadData.Repository;

namespace UploadData.Services;

public class PlanMateriaService : IPlanMateriaService
{
    private readonly IPlanMateriaRepository _repo;
    public PlanMateriaService(IPlanMateriaRepository repo) => _repo = repo;

    public async Task<UploadResult> ProcesarExcelAsync(IFormFile file, CancellationToken ct)
    {
        var res = new UploadResult();

        using var stream = file.OpenReadStream();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet(1); // primera hoja
        var used = ws.RangeUsed();
        if (used is null) return res;

        foreach (var r in used.RowsUsed().Skip(1)) // salta encabezado
        {
            res.Rows++;
            try
            {
                var row = new PlanMateriaRow
                {
                    CarCodigo = r.Cell(1).GetString().Trim(),
                    MatCodigo = r.Cell(2).GetString().Trim(),
                    Semestre  = int.Parse(r.Cell(3).GetString().Trim()),
                    Modulo    = r.Cell(4).GetString().Trim(),
                    Active    = r.Cell(5).GetString().Trim() is "1" or "true" or "TRUE",
                    CreatedBy = string.IsNullOrWhiteSpace(r.Cell(6).GetString())
                                ? "uploader"
                                : r.Cell(6).GetString().Trim()
                };

                // Validaciones mínimas
                if (string.IsNullOrWhiteSpace(row.CarCodigo)) throw new("car_codigo vacío");
                if (string.IsNullOrWhiteSpace(row.MatCodigo)) throw new("mat_codigo vacío");
                if (row.Semestre <= 0) throw new("semestre inválido");

                var action = await _repo.UpsertAsync(row, ct);
                if (string.Equals(action, "INSERT", StringComparison.OrdinalIgnoreCase))
                    res.Inserted++;
                else if (string.Equals(action, "UPDATE", StringComparison.OrdinalIgnoreCase))
                    res.Updated++;
                else
                    res.Errors.Add($"Fila {r.RowNumber()}: acción desconocida '{action}'");
            }
            catch (Exception ex)
            {
                res.Errors.Add($"Fila {r.RowNumber()}: {ex.Message}");
            }
        }
        return res;
    }
}
