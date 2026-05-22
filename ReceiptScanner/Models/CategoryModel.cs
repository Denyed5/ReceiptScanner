using System.ComponentModel.DataAnnotations;

namespace ReceiptScanner.Models
{
    public class CategoryModel
    {
        [Key]
        public string CategoryId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        public List<RItemModel> Items { get; set; } = new();
    }
}