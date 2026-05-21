using ReceiptScanner.Models;
using Tesseract;

namespace ReceiptScanner.Services
{
    public class OcrService
    {
        private readonly IWebHostEnvironment _env;

        public OcrService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<ReceiptModel> ReadText(byte[] imageBytes, string language = "bul+eng")
        {
            ReceiptModel result = new();
            var tessDataPath = Path.Combine(_env.ContentRootPath, "tessdata");

            using var engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);

            engine.DefaultPageSegMode = PageSegMode.SingleColumn;
            engine.SetVariable("user_defined_dpi", "300");

            using var pix = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix);

            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            text = CleanGarbage(text);

            result.RawText = text;

            return result;
        }
        private string CleanGarbage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            var lines = text.Split('\n');

            var cleaned = lines
                .Select(l => l.Trim())
                .Where(l =>
                    l.Length > 2 &&
                    l.Count(char.IsLetterOrDigit) > 2)
                .ToList();

            return string.Join("\n", cleaned);
        }
    }
}