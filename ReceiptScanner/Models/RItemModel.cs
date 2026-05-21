using System.ComponentModel.DataAnnotations;

namespace ReceiptScanner.Models
{
    public class RItemModel
    {
        [Key]
        public string ItemId { get; set; } = Guid.NewGuid().ToString();
        [Required(ErrorMessage = "Product name is required.")]
        public string Name { get; set; } = string.Empty;
        [Range(0.01, 999999.99, ErrorMessage = "Quantity must be greater than 0.")]
        public decimal Quantity { get; set; } = 1;
        [Range(0.01, 999999.99, ErrorMessage = "Unit price must be greater than 0.")]
        public decimal UnitPrice { get; set; }
        [Range(0.01, 999999.99, ErrorMessage = "Total price must be greater than 0.")]
        public decimal TotalPrice { get; set; }
        public bool IsWeighted { get; set; }
        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; } = "Други";
        public string? ReceiptId { get; set; } = string.Empty;
        public ReceiptModel? Receipt { get; set; }
    }
}