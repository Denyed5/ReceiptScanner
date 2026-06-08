using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReceiptScanner.Models
{
    public class RItemModel
    {
        [Key]
        public string ItemId { get; set; } = Guid.NewGuid().ToString();
        [Required(ErrorMessage = "Въведи име на продукт.")]
        public string Name { get; set; } = string.Empty;
        [Range(0.01, 999999.99, ErrorMessage = "Количество трябва да е по-голямо от 0.")]
        public decimal Quantity { get; set; } = 1;
        [Range(0.01, 999999.99, ErrorMessage = "Единична цена трябва да е по-голяма от 0.")]
        public decimal UnitPrice { get; set; }
        [Range(0.01, 999999.99, ErrorMessage = "Обща цена трябва да е по-голяма от 0.")]
        public decimal TotalPrice { get; set; }
        public bool IsWeighted { get; set; }
        public string? CategoryId { get; set; }
        public CategoryModel? Category { get; set; }
        public string? ReceiptId { get; set; } = string.Empty;
        public ReceiptModel? Receipt { get; set; }
        [NotMapped]
        public bool HasInvalidTotal =>
        Math.Round(Quantity * UnitPrice, 2) != Math.Round(TotalPrice, 2);
        [NotMapped]
        public bool IsCategorySuggested { get; set; }
    }
}