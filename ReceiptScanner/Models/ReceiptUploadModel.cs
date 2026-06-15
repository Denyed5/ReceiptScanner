using System.ComponentModel.DataAnnotations;

namespace ReceiptScanner.Models
{
    public class ReceiptUploadModel
    {
        [Required(ErrorMessage = "Моля, изберете изображение на касова бележка.")]
        public required IFormFile File { get; set; }

        public string? Description { get; set; }

        public string Language { get; set; } = "bul+eng";

        public bool UsePreprocessing { get; set; } = false;
    }
}