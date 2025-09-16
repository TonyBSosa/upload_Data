using System.Data;
using Dapper;
using UploadData.Models;

namespace UploadData.Repository;

public class PlanMateriaRepository : IPlanMateriaRepository
{
    private readonly IDbConnection _db;
    public PlanMateriaRepository(IDbConnection db) => _db = db;

    public async Task<string> UpsertAsync(PlanMateriaRow row, CancellationToken ct)
    {
        const string sql = @"
DECLARE @action TABLE (act nvarchar(10));
MERGE UNI.car_plan_materia AS tgt
USING (SELECT @car_codigo AS car_codigo, @mat_codigo AS mat_codigo) AS src
ON (tgt.car_codigo = src.car_codigo AND tgt.mat_codigo = src.mat_codigo)
WHEN MATCHED THEN
  UPDATE SET semestre=@semestre, modulo=@modulo, active=@active,
             updated_at=GETDATE(), updated_by=@created_by
WHEN NOT MATCHED THEN
  INSERT (car_codigo, mat_codigo, semestre, modulo, active, created_at, created_by)
  VALUES (@car_codigo, @mat_codigo, @semestre, @modulo, @active, GETDATE(), @created_by)
OUTPUT $action INTO @action;
SELECT TOP 1 act FROM @action;";

        var p = new
        {
            car_codigo = row.CarCodigo,
            mat_codigo = row.MatCodigo,
            semestre   = row.Semestre,
            modulo     = row.Modulo,
            active     = row.Active ? 1 : 0,
            created_by = row.CreatedBy
        };

        // Devuelve "INSERT" o "UPDATE"
        return await _db.ExecuteScalarAsync<string>(new CommandDefinition(sql, p, cancellationToken: ct));
    }
}
