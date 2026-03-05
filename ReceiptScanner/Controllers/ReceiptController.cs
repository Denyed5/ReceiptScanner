using Microsoft.AspNetCore.Mvc;
using ReceiptScanner.Models;
using ReceiptScanner.Services;

namespace ReceiptScanner.Controllers
{
    public class ReceiptController : Controller
    {
        private readonly OcrService _ocr;
        private readonly ReceiptParserService _parser;

        public ReceiptController(OcrService ocr, ReceiptParserService parser)
        {
            _ocr = ocr;
            _parser = parser;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ReceiptUploadVM model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                return View(model);
            }

            var result = await _ocr.ReadTextAsync(model.File.OpenReadStream(), model.Language);
            string? vendorLine = _parser.ExtractVendorLine(result.RawText, 5);
            string? date = _parser.ExtractDateTime(result.RawText);
            string? total = _parser.ExtractTotalSum(result.RawText);
            List<ReceiptItem> items = _parser.ExtractPurchases(result.RawText);


            result.VendorName = "Търговец: " + vendorLine;
            result.Date = "Дата: " + date;
            result.Total = "Общо: " + total;
            result.Items = items;
            return View("Result", result);
        }
    }
}
