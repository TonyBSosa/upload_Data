// File: Services/UploadService.cs
using System.Data;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UploadData.Services;

public class UploadService : IUploadService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<UploadService> _logger;

    public UploadService(IConfiguration cfg, ILogger<UploadService> logger)
    {
        _cfg = cfg;
        _logger = logger;
    }

    /// <summary>
    /// Lee un Excel y hace bulk insert 1:1 en STG.ofertas_raw (o la tabla configurada).
    /// - sheet: nombre o índice 1-based de la hoja. Si es null/vacío/"string", usa la primera.
    /// - semestre: si viene, sobreescribe la columna 'semestre' destino.
    /// </summary>
    public async Task<UploadResult> ProcessExcelToStagingAsync(
        string filePath, string? sheet, int? semestre, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("No se encontró el archivo a procesar.", filePath);

        // --- Configuración ---
        var connName = _cfg.GetValue<string>("Upload:ConnectionName") ?? "Default";
        var connStr  = _cfg.GetConnectionString(connName)
                       ?? throw new InvalidOperationException($"No existe ConnectionStrings:{connName}.");

        var destTable = _cfg.GetValue<string>("Upload:DestinationTable") ?? "STG.ofertas_raw";

        // Mapa Excel->SQL desde appsettings.json
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in _cfg.GetSection("Upload:ColumnMap").GetChildren())
            map[child.Key] = child.Value!;
        if (map.Count == 0)
            throw new InvalidOperationException("Upload:ColumnMap está vacío en appsettings.json.");

        // --- Abrir Excel ---
        using var wb = new XLWorkbook(filePath);
        var ws = SelectWorksheet(wb, sheet);

        // --- Construir DataTable con columnas destino ---
        var dt = new DataTable("ofertas_raw");
        foreach (var dest in map.Values.Distinct())
            dt.Columns.Add(dest, typeof(string));

        // Metadatos presentes en tu tabla STG.ofertas_raw
        dt.Columns.Add("source_file", typeof(string)); // nvarchar(260)
        dt.Columns.Add("batch_id", typeof(Guid));      // uniqueidentifier NOT NULL
        dt.Columns.Add("loaded_at", typeof(DateTime)); // datetime2(7)   NOT NULL

        // --- Encabezados ---
        var firstRow = ws.FirstRowUsed() ?? throw new InvalidOperationException("La hoja está vacía.");
        var headers  = firstRow.CellsUsed().Select(c => c.GetString().Trim()).ToList();
        var colCount = headers.Count;

        // --- Cargar filas ---
        var batchId  = Guid.NewGuid();
        int rowsRead = 0;

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            if (ct.IsCancellationRequested) ct.ThrowIfCancellationRequested();

            var dr = dt.NewRow();

            for (int i = 0; i < colCount; i++)
            {
                var header = headers[i];
                if (!map.TryGetValue(header, out var destCol))
                    continue; // encabezado no mapeado -> se ignora

                var val = row.Cell(i + 1).GetValue<string>()?.Trim();
                dr[destCol] = val ?? string.Empty;
            }

            // Sobrescribir 'semestre' si viene como parámetro
            if (semestre.HasValue && dt.Columns.Contains("semestre"))
                dr["semestre"] = semestre.Value.ToString();

            dr["source_file"] = Path.GetFileName(filePath);
            dr["batch_id"]    = batchId;
            dr["loaded_at"]   = DateTime.UtcNow;

            dt.Rows.Add(dr);
            rowsRead++;
        }

        // Quitar filas totalmente vacías (todas las string en blanco)
        var stringCols = dt.Columns.Cast<DataColumn>().Where(c => c.DataType == typeof(string)).ToArray();
        var empties = dt.AsEnumerable()
                        .Where(r => stringCols.All(c => string.IsNullOrWhiteSpace(r[c]?.ToString())))
                        .ToList();
        foreach (var r in empties) dt.Rows.Remove(r);

        // --- Bulk insert ---
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        // (Opcional) Validación: columnas existen en la tabla destino
        await ValidateDestinationColumnsAsync(conn, destTable, dt, ct);

        int inserted;
        using (var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.CheckConstraints, null)
               { DestinationTableName = destTable, BatchSize = 5000 })
        {
            foreach (DataColumn c in dt.Columns)
                bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);

            await bulk.WriteToServerAsync(dt, ct);
            inserted = dt.Rows.Count;
        }

        // Log BD conectada
        using (var cmd = new SqlCommand("SELECT DB_NAME()", conn))
        {
            var db = (string)await cmd.ExecuteScalarAsync(ct);
            _logger.LogInformation("Bulk a {Table} en BD {Db}: {Read} leídas, {Inserted} insertadas, batch {BatchId}",
                destTable, db, rowsRead, inserted, batchId);
        }

        return new UploadResult(rowsRead, inserted, batchId);
    }

    // --- Helpers ---

    private static IXLWorksheet SelectWorksheet(XLWorkbook wb, string? sheetParam)
    {
        var requested = sheetParam?.Trim();

        // 1) Si vacío o "string" (placeholder de Swagger) -> primera hoja
        if (string.IsNullOrWhiteSpace(requested) ||
            string.Equals(requested, "string", StringComparison.OrdinalIgnoreCase))
        {
            return wb.Worksheet(1);
        }

        // 2) Si es número válido -> índice 1-based
        if (int.TryParse(requested, out var idx) && idx >= 1 && idx <= wb.Worksheets.Count)
        {
            return wb.Worksheet(idx);
        }

        // 3) Si existe por nombre
        if (wb.Worksheets.TryGetWorksheet(requested, out var byName))
        {
            return byName;
        }

        // 4) Error descriptivo con lista de hojas disponibles
        var disponibles = string.Join(", ", wb.Worksheets.Select(w => $"\"{w.Name}\""));
        throw new ArgumentException($"La hoja \"{requested}\" no existe. Hojas disponibles: {disponibles}");
    }

    private static async Task ValidateDestinationColumnsAsync(
        SqlConnection conn, string destTable, DataTable dt, CancellationToken ct)
    {
        var destCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        const string sqlCols = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = PARSENAME(@t, 2)
              AND TABLE_NAME   = PARSENAME(@t, 1)
        """;

        using (var cmd = new SqlCommand(sqlCols, conn))
        {
            cmd.Parameters.AddWithValue("@t", destTable.Replace("[","").Replace("]",""));
            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct)) destCols.Add(rd.GetString(0));
        }

        var missing = dt.Columns.Cast<DataColumn>()
                      .Select(c => c.ColumnName)
                      .Where(c => !destCols.Contains(c))
                      .ToList();

        if (missing.Any())
            throw new InvalidOperationException(
                $"Columnas destino inexistentes en {destTable}: {string.Join(", ", missing)}");
    }
}
