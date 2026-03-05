using System.ComponentModel.DataAnnotations;

namespace ReceiptScanner.Models
{
    public class ReceiptUploadVM
    {
        [Required]
        public IFormFile File { get; set; }

        public string? description { get; set; }

        public string Language { get; set; } = "bul+eng";
    }
}
