using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReceiptScanner.Data;
using ReceiptScanner.Models;
using System.Security.Claims;

namespace ReceiptScanner.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ReceiptScannerContext _context;

        public DashboardController(ReceiptScannerContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var receipts = await _context.Receipts
                .Include(r => r.Items)
                .ThenInclude(i => i.Category)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var categoryTotals = receipts
                .SelectMany(r => r.Items)
                .Where(i => i.Category != null)
                .GroupBy(i => i.Category!.Name)
                .OrderByDescending(g => g.Sum(i => i.TotalPrice))
                .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalPrice));

            var monthlyTotals = receipts
                .Where(r => r.Date.HasValue)
                .GroupBy(r => r.Date!.Value.ToString("MMM yyyy"))
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalBGN ?? 0));

            var model = new DashboardModel
            {
                ReceiptCount = receipts.Count,

                TotalSpentBGN = receipts.Sum(r => r.TotalBGN ?? 0),

                ThisMonthSpentBGN = receipts
                    .Where(r => r.Date.HasValue && r.Date.Value >= monthStart)
                    .Sum(r => r.TotalBGN ?? 0),

                TotalSpentEUR = receipts.Sum(r => r.TotalEUR ?? 0),

                ThisMonthSpentEUR = receipts
                    .Where(r => r.Date.HasValue && r.Date.Value >= monthStart)
                    .Sum(r => r.TotalEUR ?? 0),

                TopCategory = categoryTotals.FirstOrDefault().Key ?? "No data",

                RecentReceipts = receipts.Take(5).ToList(),

                CategoryTotals = categoryTotals,

                MonthlyTotals = monthlyTotals
            };

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Analytics(DateTime? startDate, DateTime? endDate, string? period = null)
        { 
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var receiptsQuery = _context.Receipts
                .Include(r => r.Items)
                .ThenInclude(i => i.Category)
                .Where(r => r.UserId == userId);

            var today = DateTime.Today;

            switch (period)
            {
                case "week":
                    startDate = today.AddDays(-(int)today.DayOfWeek + 1);
                    endDate = today;
                    break;

                case "month":
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = today;
                    break;

                case "30days":
                    startDate = today.AddDays(-30);
                    endDate = today;
                    break;

                case "year":
                    startDate = new DateTime(today.Year, 1, 1);
                    endDate = today;
                    break;
            }

            if (startDate.HasValue)
            {
                receiptsQuery = receiptsQuery
                    .Where(r => r.Date.HasValue &&
                                r.Date.Value >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                receiptsQuery = receiptsQuery
                    .Where(r => r.Date.HasValue &&
                                r.Date.Value <= endDate.Value.AddDays(1).AddTicks(-1));
            }

            var receipts = await receiptsQuery
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            var categoryTotals = receipts
                .SelectMany(r => r.Items)
                .Where(i => i.Category != null)
                .GroupBy(i => i.Category!.Name)
                .ToDictionary(g => g.Key,
                              g => g.Sum(i => i.TotalPrice));

            var monthlyTotals = receipts
                .Where(r => r.Date.HasValue)
                .GroupBy(r => r.Date!.Value.ToString("MMM yyyy"))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r => r.TotalEUR ?? 0));

            var totalSpentEURPeriod = receipts.Sum(r => r.TotalEUR ?? 0);
            var totalSpentBGNPeriod = receipts.Sum(r => r.TotalBGN ?? 0);

            var averageSpentEURPeriod = receipts.Count > 0
                ? totalSpentEURPeriod / receipts.Count
                : 0;
            var averageSpentBGNPeriod = receipts.Count > 0
                ? totalSpentBGNPeriod / receipts.Count
                : 0;

            if (startDate.HasValue && endDate.HasValue)
            {
                ViewBag.PeriodText =
                    $"{startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}";
            }
            else
            {
                ViewBag.PeriodText = "Всички налични данни";
            }

            var model = new DashboardModel
            {
                TotalSpentBGNPeriod = totalSpentBGNPeriod,
                AverageReceiptBGNPeriod = averageSpentBGNPeriod,
                TotalSpentEURPeriod = totalSpentEURPeriod,
                AverageReceiptEURPeriod = Math.Round(averageSpentEURPeriod, 2),
                ReceiptCount = receipts.Count,
                CategoryTotals = categoryTotals,
                MonthlyTotals = monthlyTotals
            };

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(model);
        }
    }
}