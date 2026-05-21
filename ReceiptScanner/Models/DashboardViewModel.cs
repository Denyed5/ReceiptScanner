using ReceiptScanner.Models;

namespace ReceiptScanner.Models
{
    public class DashboardViewModel
    {
        public int ReceiptCount { get; set; }
        public decimal TotalSpentBGN { get; set; }
        public decimal ThisMonthSpentBGN { get; set; }
        public string TopCategory { get; set; } = "No data";
        public List<ReceiptModel> RecentReceipts { get; set; } = new();
        public Dictionary<string, decimal> CategoryTotals { get; set; } = new();
        public Dictionary<string, decimal> MonthlyTotals { get; set; } = new();
    }
}