namespace ReceiptScanner.Models
{
    public class ReceiptItem
    {
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsWeighted { get; set; }
    }
}