using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using ReceiptScanner.Data;
using ReceiptScanner.Models;
using ReceiptScanner.Services;
using System.Security.Claims;

namespace ReceiptScanner.Controllers
{
    public class ReceiptController : Controller
    {
        private const decimal EurToBgnRate = 1.95583m;

        private readonly OcrService _ocr;
        private readonly ReceiptParserService _parser;
        private readonly ImagePreprocessingService _preprocessing;
        private readonly ReceiptScannerContext _context;

        public ReceiptController(OcrService ocr, ReceiptParserService parser,
                                 ImagePreprocessingService preprocessing, ReceiptScannerContext context)
        {
            _ocr = ocr;
            _parser = parser;
            _preprocessing = preprocessing;
            _context = context;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> History(string sortBy = "date", string sortDirection = "desc")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            var receiptsQuery = _context.Receipts
                .Where(r => r.UserId == userId);

            receiptsQuery = (sortBy.ToLowerInvariant(), sortDirection.ToLowerInvariant()) switch
            {
                ("date", "asc") => receiptsQuery.OrderBy(r => r.Date),
                ("total", "asc") => receiptsQuery.OrderBy(r => r.TotalBGN),
                ("total", _) => receiptsQuery.OrderByDescending(r => r.TotalBGN),
                _ => receiptsQuery.OrderByDescending(r => r.Date)
            };

            ViewBag.SortBy = sortBy;
            ViewBag.SortDirection = sortDirection;

            var receipts = await receiptsQuery.ToListAsync();

            return View(receipts);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            var receipt = await _context.Receipts
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.ReceiptId == id && r.UserId == userId);

            if (receipt == null)
            {
                return NotFound();
            }

            return View(receipt);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            var receipt = await _context.Receipts
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.ReceiptId == id && r.UserId == userId);

            if (receipt == null)
            {
                return NotFound();
            }

            return View(receipt);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ReceiptModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            ApplyCurrencyConversion(model);

            ModelState.Remove(nameof(ReceiptModel.TotalBGN));
            ModelState.Remove(nameof(ReceiptModel.TotalEUR));
            ModelState.Remove(nameof(ReceiptModel.UserId));
            ModelState.Remove(nameof(ReceiptModel.User));

            if (!model.TotalBGN.HasValue)
            {
                ModelState.AddModelError(nameof(ReceiptModel.TotalBGN), "Total in BGN is required.");
            }

            if (!model.TotalEUR.HasValue)
            {
                ModelState.AddModelError(nameof(ReceiptModel.TotalEUR), "Total in EUR is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var receipt = await _context.Receipts
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.ReceiptId == model.ReceiptId && r.UserId == userId);

            if (receipt == null)
            {
                return NotFound();
            }

            receipt.VendorName = model.VendorName;
            receipt.Date = model.Date;
            receipt.TotalBGN = model.TotalBGN;
            receipt.TotalEUR = model.TotalEUR;
            receipt.RawText = model.RawText;

            _context.ReceiptItems.RemoveRange(receipt.Items);
            receipt.Items = model.Items ?? new List<RItemModel>();

            foreach (var item in receipt.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    item.ItemId = Guid.NewGuid().ToString();
                }

                item.ReceiptId = receipt.ReceiptId;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            var receipt = await _context.Receipts
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.ReceiptId == id && r.UserId == userId);

            if (receipt == null)
            {
                return NotFound();
            }

            _context.ReceiptItems.RemoveRange(receipt.Items);
            _context.Receipts.Remove(receipt);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(History));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ReceiptUploadModel model, string? croppedImage)
        {
            byte[] imageBytes;

            if (!string.IsNullOrEmpty(croppedImage))
            {
                var base64Data = croppedImage.Split(',')[1];
                imageBytes = Convert.FromBase64String(base64Data);
            }
            else
            {
                if (model.File == null || model.File.Length == 0)
                {
                    return View(model);
                }

                using var ms = new MemoryStream();
                await model.File.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            byte[] finalBytes;

            if (model.UsePreprocessing)
            {
                Mat image = Cv2.ImDecode(imageBytes, ImreadModes.Color);
                var processed = _preprocessing.Preprocess(image);
                finalBytes = processed.ToBytes(".png");
            }
            else
            {
                finalBytes = imageBytes;
            }

            var result = await _ocr.ReadText(finalBytes, model.Language);

            string? vendorLine = _parser.ExtractVendorLine(result.RawText, 5);
            if (vendorLine == "Не може да бъде извлечен.")
            {
                vendorLine = null;
            }
            DateTime? date = _parser.ExtractDateTime(result.RawText);
            decimal? totalBGN = _parser.ExtractTotalSumBGN(result.RawText);
            decimal? totalEUR = _parser.ExtractTotalSumEUR(result.RawText);
            List<RItemModel> items = _parser.ExtractPurchases(result.RawText);
            List<string>? cleanLines = _parser.GetCleanLines(result.RawText);

            result.RawTextLines = cleanLines;
            result.VendorName = vendorLine;
            result.Date = date;
            result.TotalBGN = totalBGN;
            result.TotalEUR = totalEUR;
            result.Items = items;

            ApplySuggestedValues(result);

            return View("Result", result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceipt(ReceiptModel model)
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return Redirect("/Identity/Account/Login");
            }

            ApplyCurrencyConversion(model);
            model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            ModelState.Remove(nameof(ReceiptModel.TotalBGN));
            ModelState.Remove(nameof(ReceiptModel.TotalEUR));

            if (!model.TotalBGN.HasValue)
            {
                ModelState.AddModelError(nameof(ReceiptModel.TotalBGN), "Total in BGN is required.");
            }

            if (!model.TotalEUR.HasValue)
            {
                ModelState.AddModelError(nameof(ReceiptModel.TotalEUR), "Total in EUR is required.");
            }

            if (!ModelState.IsValid)
            {
                return View("Result", model);
            }

            _context.Receipts.Add(model);
            await _context.SaveChangesAsync();

            return View("Saved");
        }

        private static void ApplySuggestedValues(ReceiptModel receipt)
        {
            if (!receipt.Date.HasValue)
            {
                receipt.Date = DateTime.Now;
                receipt.IsDateSuggested = true;
            }

            if (receipt.TotalBGN.HasValue && !receipt.TotalEUR.HasValue)
            {
                receipt.TotalEUR = Math.Round(receipt.TotalBGN.Value / EurToBgnRate, 2);
                receipt.IsTotalEURSuggested = true;
            }

            if (receipt.TotalEUR.HasValue && !receipt.TotalBGN.HasValue)
            {
                receipt.TotalBGN = Math.Round(receipt.TotalEUR.Value * EurToBgnRate, 2);
                receipt.IsTotalBGNSuggested = true;
            }
        }

        private static void ApplyCurrencyConversion(ReceiptModel receipt)
        {
            if (receipt.TotalBGN.HasValue && !receipt.TotalEUR.HasValue)
            {
                receipt.TotalEUR = Math.Round(receipt.TotalBGN.Value / EurToBgnRate, 2);
                receipt.IsTotalEURSuggested = true;
            }

            if (receipt.TotalEUR.HasValue && !receipt.TotalBGN.HasValue)
            {
                receipt.TotalBGN = Math.Round(receipt.TotalEUR.Value * EurToBgnRate, 2);
                receipt.IsTotalBGNSuggested = true;
            }
        }
    }
}
