namespace UploadData.Services;

public interface IUploadService
{
    Task<UploadResult> ProcessExcelToStagingAsync(
        string filePath, string? sheet, int? semestre, CancellationToken ct);
}

public record UploadResult(int RowsRead, int RowsInserted, Guid BatchId);
