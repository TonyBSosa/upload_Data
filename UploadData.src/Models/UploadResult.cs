 namespace UploadData.Models;

public class UploadResult
{
     public int Rows { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public List<string> Errors { get; set; } = new();

     public string? TipoArchivo { get; set; }
    public long? TamañoKb { get; set; }
    public string? Mensaje { get; set; }
}
