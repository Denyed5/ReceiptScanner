using System.Runtime.CompilerServices;

namespace ReceiptScanner.Models
{
    public class ReceiptResultVM
    {
        public string? VendorName { get; set;}  
        public string? Total { get; set; }
        public string? Date { get; set; }
        public string RawText { get; set; } = "";
        public float? Confidence { get; set; }
        public List<ReceiptItem> Items { get; set; } = new();
    }
}
