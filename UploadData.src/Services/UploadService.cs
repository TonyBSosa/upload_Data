using System.Text;
using YourApp.Models;

namespace YourApp.Services;

public class UploadService : IUploadService
{
    private readonly string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
    private readonly string logFilePath;

    public UploadService()
    {
        Directory.CreateDirectory(logDirectory);

        logFilePath = Path.Combine(logDirectory, "uploads.log");
    }

    public async Task<UploadResult> ProcesarArchivoAsync(IFormFile archivo, string tipo)
    {
        if (archivo == null || archivo.Length == 0)
            throw new ArgumentException("El archivo está vacío");

        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms);
        var tamañoKb = ms.Length / 1024;

        await GuardarLogAsync(tipo, archivo.FileName, tamañoKb);

        return new UploadResult
        {
            TipoArchivo = tipo,
            TamañoKb = tamañoKb,
            Mensaje = "Archivo procesado correctamente"
        };
    }

    private async Task GuardarLogAsync(string tipo, string nombreArchivo, long tamañoKb)
    {
        var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}" +
                       $" | Tipo: {tipo} | " +
                       $"Archivo: {nombreArchivo} | " +
                       $"Tamaño: {tamañoKb} KB{Environment.NewLine}";
        await File.AppendAllTextAsync(logFilePath, logEntry, Encoding.UTF8);
    }
}