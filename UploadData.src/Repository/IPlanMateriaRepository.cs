using System.Threading;
using System.Threading.Tasks;
using UploadData.Models;

namespace UploadData.Repository;

public interface IPlanMateriaRepository
{
    /// <summary>
    /// Ejecuta un UPSERT por (car_codigo, mat_codigo) y devuelve "INSERT" o "UPDATE".
    /// </summary>
    Task<string> UpsertAsync(PlanMateriaRow row, CancellationToken ct);
}
