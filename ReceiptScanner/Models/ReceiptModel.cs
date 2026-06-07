using Microsoft.EntityFrameworkCore;
using ReceiptScanner.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReceiptScanner.Models
{
    [Index(nameof(UserId))]
    public class ReceiptModel
    {
        [Key]
        public string ReceiptId { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Vendor is required.")]
        public string? VendorName { get; set; }

        [Required(ErrorMessage = "Total in BGN is required.")]
        [Range(0.01, 999999.99, ErrorMessage = "Total in BGN must be greater than 0.")]
        public decimal? TotalBGN { get; set; }

        [Required(ErrorMessage = "Total in EUR is required.")]
        [Range(0.01, 999999.99, ErrorMessage = "Total in EUR must be greater than 0.")]
        public decimal? TotalEUR { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime? Date { get; set; }
        public string RawText { get; set; } = "";
        public List<RItemModel> Items { get; set; } = new();
        public List<string> RawTextLines { get; set; } = new();
        public string? ImagePath { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ReceiptScannerUser? User { get; set; }
        [NotMapped]
        public float OcrConfidence { get; set; }

        [NotMapped]
        public bool IsDateSuggested { get; set; }

        [NotMapped]
        public bool IsTotalBGNSuggested { get; set; }

        [NotMapped]
        public bool IsTotalEURSuggested { get; set; }
    }
}