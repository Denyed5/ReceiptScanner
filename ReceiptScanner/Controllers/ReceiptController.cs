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
        private const string UnsupportedImageMessage = "Неподдържан формат. Системата поддържа само JPG, PNG и BMP изображения.";

        private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".bmp"
        };

        private static readonly HashSet<string> SupportedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/bmp"
        };

        private readonly OcrService _ocr;
        private readonly ReceiptParserService _parser;
        private readonly ImagePreprocessingService _preprocessing;
        private readonly ReceiptScannerContext _context;
        private readonly CategoryDetectionService _categoryDetectionService;
        private readonly IWebHostEnvironment _env;
        public ReceiptController(OcrService ocr, ReceiptParserService parser,
                                 ImagePreprocessingService preprocessing, ReceiptScannerContext context, 
                                 CategoryDetectionService categoryDetectionService,
                                 IWebHostEnvironment env)
        {
            _ocr = ocr;
            _parser = parser;
            _preprocessing = preprocessing;
            _context = context;
            _categoryDetectionService = categoryDetectionService;
            _env = env;
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
                .ThenInclude(i => i.Category)
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
                .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(r => r.ReceiptId == id && r.UserId == userId);

            if (receipt == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(receipt);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Image(string id)
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
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ReceiptId == id && r.UserId == userId);

            if (receipt == null || string.IsNullOrWhiteSpace(receipt.ImagePath))
            {
                return NotFound();
            }

            var fullPath = GetStoredImageFullPath(receipt.ImagePath);
            var allowedRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "App_Data", "ReceiptImages"));

            if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            return PhysicalFile(fullPath, "image/png");
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
                ModelState.AddModelError(nameof(ReceiptModel.TotalBGN), "Обща сума в лева е задължителна.");
            }

            if (!model.TotalEUR.HasValue)
            {
                ModelState.AddModelError(nameof(ReceiptModel.TotalEUR), "Обща сума в евро е задължителна.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();

                return View(model);
            }

            var receipt = await _context.Receipts
                .Include(r => r.Items)
                .ThenInclude(i => i.Category)
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

            receipt.Items.Clear();

            foreach (var item in model.Items ?? new List<RItemModel>())
            {
                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    item.ItemId = Guid.NewGuid().ToString();
                }

                item.ReceiptId = receipt.ReceiptId;

                item.Category = null;

                receipt.Items.Add(item);
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

            if (!string.IsNullOrWhiteSpace(receipt.ImagePath))
            {
                var imageFullPath = GetStoredImageFullPath(receipt.ImagePath);
                var allowedRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "App_Data", "ReceiptImages"));

                if (imageFullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase)
                    && System.IO.File.Exists(imageFullPath))
                {
                    System.IO.File.Delete(imageFullPath);
                }
            }

            return RedirectToAction(nameof(History));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ReceiptUploadModel model, string? croppedImage)
        {
            byte[] imageBytes;

            if (!string.IsNullOrEmpty(croppedImage)) 
            {
                try
                {
                    var base64Data = croppedImage.Split(',', 2)[1];
                    imageBytes = Convert.FromBase64String(base64Data);
                }
                catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException)
                {
                    return UploadViewWithError(model, "Изображението не може да бъде прочетено.");
                }
            }
            else
            {
                if (model.File == null || model.File.Length == 0)
                {
                    return View(model);
                }

                if (!IsSupportedImageFile(model.File))
                {
                    return UploadViewWithError(model, UnsupportedImageMessage);
                }

                using var ms = new MemoryStream();
                await model.File.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            Mat decodedImage;
            try
            {
                decodedImage = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            }
            catch (OpenCVException)
            {
                return UploadViewWithError(model, "The selected file could not be loaded as an image. Please choose a different receipt image.");
            }

            using Mat image = decodedImage;
            if (image.Empty())
            {
                return UploadViewWithError(model, "The selected file could not be loaded as an image. Please choose a different receipt image.");
            }

            byte[] finalBytes;

            if (model.UsePreprocessing)
            {
                using var processed = _preprocessing.Preprocess(image);
                finalBytes = processed.ToBytes(".png");
            }
            else
            {
                finalBytes = image.ToBytes(".png");
            }

            var tempImageDirectory = Path.Combine(_env.ContentRootPath, "App_Data", "ReceiptImages", "Temp");
            Directory.CreateDirectory(tempImageDirectory);

            var tempImageFileName = $"{Guid.NewGuid():N}.png";
            var tempImageFullPath = Path.Combine(tempImageDirectory, tempImageFileName);

            await System.IO.File.WriteAllBytesAsync(tempImageFullPath, finalBytes);

            var tempImagePath = Path.Combine("App_Data", "ReceiptImages", "Temp", tempImageFileName);

            var result = await _ocr.ReadText(finalBytes, model.Language);

            result.ImagePath = tempImagePath;

            var lines = _parser.GetCleanLines(result.RawText);
            string? vendorLine = _parser.ExtractVendorLine(lines, 5);
            if (vendorLine == "Не може да бъде извлечен.")
            {
                vendorLine = null;
            }
            DateTime? date = _parser.ExtractDateTime(lines);
            decimal? totalBGN = _parser.ExtractTotalSumBGN(lines);
            decimal? totalEUR = _parser.ExtractTotalSumEUR(lines);
            List<RItemModel> items = _parser.ExtractPurchases(lines);

            foreach (var item in items)
            {
                var detection =
                    await _categoryDetectionService
                        .DetectCategoryAsync(item.Name);

                item.CategoryId =
                    detection.CategoryId;

                item.IsCategorySuggested =
                    detection.IsSuggested;
            }

            List<string>? cleanLines = _parser.GetCleanLines(result.RawText);

            result.RawTextLines = cleanLines;
            result.VendorName = vendorLine;
            result.Date = date;
            result.TotalBGN = totalBGN;
            result.TotalEUR = totalEUR;
            result.Items = items;

            ApplySuggestedValues(result);

            ViewBag.Categories = await _context.Categories.ToListAsync();

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
                ViewBag.Categories = await _context.Categories.ToListAsync();

                return View("Result", model);
            }

            model.ImagePath = MoveTempReceiptImageToUserStorage(model.ImagePath, model.UserId, model.ReceiptId);

            foreach (var item in model.Items)
            {
                item.Category = null;
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

        private static bool IsSupportedImageFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName);
            var hasSupportedExtension = SupportedImageExtensions.Contains(extension);
            var hasSupportedContentType = !string.IsNullOrWhiteSpace(file.ContentType)
                && SupportedImageContentTypes.Contains(file.ContentType);

            return hasSupportedExtension || hasSupportedContentType;
        }

        private IActionResult UploadViewWithError(ReceiptUploadModel model, string message)
        {
            ViewBag.UploadErrorMessage = message;
            return View("Upload", model);
        }

        private string GetReceiptImagesDirectory(string userId)
        {
            return Path.Combine(_env.ContentRootPath, "App_Data", "ReceiptImages", GetSafePathSegment(userId));
        }

        private string GetReceiptImageRelativePath(string userId, string fileName)
        {
            return Path.Combine("App_Data", "ReceiptImages", GetSafePathSegment(userId), fileName);
        }

        private string GetStoredImageFullPath(string imagePath)
        {
            return Path.GetFullPath(Path.Combine(_env.ContentRootPath, imagePath));
        }

        private string? MoveTempReceiptImageToUserStorage(string? tempImagePath, string userId, string receiptId)
        {
            if (string.IsNullOrWhiteSpace(tempImagePath))
            {
                return null;
            }

            var tempFullPath = GetStoredImageFullPath(tempImagePath);
            var tempRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "App_Data", "ReceiptImages", "Temp"));

            if (!tempFullPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
                || !System.IO.File.Exists(tempFullPath))
            {
                return null;
            }

            var userImageDirectory = GetReceiptImagesDirectory(userId);
            Directory.CreateDirectory(userImageDirectory);

            var finalFileName = $"{receiptId}.png";
            var finalFullPath = Path.Combine(userImageDirectory, finalFileName);

            System.IO.File.Move(tempFullPath, finalFullPath, overwrite: true);

            return GetReceiptImageRelativePath(userId, finalFileName);
        }

        private static string GetSafePathSegment(string value)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '_');
            }

            return value;
        }
    }
}
