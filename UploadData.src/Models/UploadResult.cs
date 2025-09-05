namespace YourApp.Models;

public class UploadResult
{
    public string TipoArchivo { get; set; } = default!;
    public long TamañoKb { get; set; }
    public string Mensaje { get; set; } = default!; 
}