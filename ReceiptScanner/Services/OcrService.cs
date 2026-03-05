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

        public async Task<ReceiptResultVM> ReadTextAsync(Stream imageStream, string language = "bul")
        {
            ReceiptResultVM result = new();
            var tessDataPath = Path.Combine(_env.ContentRootPath, "tessdata");

            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            using var engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);

            engine.DefaultPageSegMode = PageSegMode.SingleColumn; // SparseText/SingleColumn works best

            using var pix = Pix.LoadFromMemory(bytes);
            using var page = engine.Process(pix);
            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            result.RawText = text;
            result.Confidence = confidence * 100;

            return result;
        }
    }
}
