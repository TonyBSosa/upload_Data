using Microsoft.AspNetCore.Http;

namespace UploadData.Models
{
    // Representa el form-data que envías desde Swagger/Front
    public class UploadFileRequest
    {
        public IFormFile Archivo { get; set; } = default!;
    }
}
