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

            var model = new DashboardViewModel
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
        public async Task<IActionResult> Analytics()
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

            var categoryTotals = receipts
                .SelectMany(r => r.Items)
                .Where(i => i.Category != null)
                .GroupBy(i => i.Category!.Name)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(i => i.TotalPrice)
                );

            var monthlyTotals = receipts
                .Where(r => r.Date.HasValue)
                .GroupBy(r => r.Date!.Value.ToString("MMM yyyy"))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r => r.TotalBGN ?? 0)
                );

            var model = new DashboardViewModel
            {
                CategoryTotals = categoryTotals,
                MonthlyTotals = monthlyTotals
            };

            return View(model);
        }
    }
}