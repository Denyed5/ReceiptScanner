using ReceiptScanner.Models;
using System.Text.RegularExpressions;

namespace ReceiptScanner.Services
{
    public class ReceiptParserService
    {
        private static readonly string[] LegalKeywordsVendor =
        {
            "ЕООД", "ООД", "АД",
            "ЕТ", "КД", "Пацони",
            "Кауфланд", "Лидл"
        };

        private static readonly string[] IgnoreKeywordsVendor =
        {
            "БУЛСТАТ", "ЕИК", "ДДС", "ЗДДС", "BG",
            "ГР.", "УЛ.", "БУЛ.", "АДРЕС",
            "ФИСКАЛЕН", "КАСОВ", "БОН",
            "ОПЕРАТОР", "КАСИЕР"
        };

        private static readonly string[] LegalKeywordsTotal =
        {
            "ОБЩО", "ОБЩА СУМА",
            "TOTAL", "ТОТАЛ"
        };

        private static readonly string[] LegalKeywordsTotalBGN =
        {
            "ЛВ", "BGN", "ЛЕВА", "LV", "LEVA", "ЛB"
        };

        private static readonly string[] LegalKeywordsTotalEUR =
        {
            "ЕВРО", "EUR", "EURO"
        };

        private static readonly Regex QuantityRegex =
            new Regex(@"\d+[.,]\d+\s*x\s*\d+[.,]\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PriceRegex =
            new Regex(@"\d+[.,]\d{2}", RegexOptions.Compiled);

        public string? ExtractVendorLine(string rawText, int headerLinesCount)
        {
            var lines = GetCleanLines(rawText)
                .Take(headerLinesCount)
                .ToList();

            var cleaned = lines
                .Where(l => !ContainsAny(l, IgnoreKeywordsVendor))
                .ToList();

            if (cleaned.Count == 0)
                return null;

            var legal = cleaned
                .FirstOrDefault(l => ContainsAny(l, LegalKeywordsVendor));

            if (!string.IsNullOrWhiteSpace(legal))
                return legal;

            return null;
        }

        public List<string> GetCleanLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            text = text.Replace("\r\n", "\n")
                       .Replace('\r', '\n');

            return text
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
        }

        private static bool ContainsAny(string line, string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                if (line.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public DateTime? ExtractDateTime(string rawText)
        {
            var regex = new Regex(@"\d{1,2}[./-]\d{1,2}[./-]\d{2,4}(\s+\d{1,2}:\d{2}(:\d{2})?)?");
            var lines = GetCleanLines(rawText);
            var culture = new System.Globalization.CultureInfo("bg-BG");

            foreach (var line in lines)
            {
                var match = regex.Match(line);

                if (match.Success)
                {
                    var dateStr = match.Value;

                    if (DateTime.TryParse(dateStr, culture, System.Globalization.DateTimeStyles.None, out var dt))
                        return dt;
                }
            }

            return null;
        }

        public decimal? ExtractTotalSumBGN(string rawText)
        {
            var lines = GetCleanLines(rawText);
            var exactCurrencyTotal = ExtractTotalByCurrency(lines, LegalKeywordsTotalBGN);

            if (exactCurrencyTotal.HasValue)
                return exactCurrencyTotal;

            return ExtractTotalWithoutCurrency(lines);
        }

        public decimal? ExtractTotalSumEUR(string rawText)
        {
            var lines = GetCleanLines(rawText);
            return ExtractTotalByCurrency(lines, LegalKeywordsTotalEUR);
        }

        private static decimal? ExtractTotalByCurrency(List<string> lines, string[] currencyKeywords)
        {
            foreach (var line in lines)
            {
                if (!ContainsAny(line, LegalKeywordsTotal) || !ContainsAny(line, currencyKeywords))
                    continue;

                var prices = PriceRegex.Matches(line)
                    .Select(m => ParsePrice(m.Value))
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();

                if (prices.Count > 0)
                    return prices[^1];
            }

            return null;
        }

        private static decimal? ExtractTotalWithoutCurrency(List<string> lines)
        {
            foreach (var line in lines)
            {
                if (!ContainsAny(line, LegalKeywordsTotal))
                    continue;

                if (ContainsAny(line, LegalKeywordsTotalEUR))
                    continue;

                var prices = PriceRegex.Matches(line)
                    .Select(m => ParsePrice(m.Value))
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();

                if (prices.Count > 0)
                    return prices[^1];
            }

            return null;
        }

        private static decimal? ParsePrice(string value)
        {
            if (decimal.TryParse(
                value.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal result))
            {
                return result;
            }

            return null;
        }

        public List<RItemModel> ExtractPurchases(string rawText)
        {
            var lines = GetCleanLines(rawText);

            int totalIndex = lines.FindIndex(l => ContainsAny(l, LegalKeywordsTotal));
            if (totalIndex < 0) return new List<RItemModel>();

            var items = new List<RItemModel>();

            decimal tempQty = 1;
            decimal tempUnitPrice = 0;
            bool hasQuantity = false;

            for (int i = 0; i < totalIndex; i++)
            {
                var line = lines[i];

                if (QuantityRegex.IsMatch(line))
                {
                    var numbers = Regex.Matches(line, @"\d+[.,]\d+")
                        .Select(m => decimal.Parse(m.Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture))
                        .ToList();

                    if (numbers.Count == 2)
                    {
                        tempQty = numbers[0];
                        tempUnitPrice = numbers[1];
                        hasQuantity = true;
                    }

                    continue;
                }

                var priceMatch = PriceRegex.Match(line);

                if (!priceMatch.Success)
                    continue;

                decimal totalPrice = decimal.Parse(
                    priceMatch.Value.Replace(',', '.'),
                    System.Globalization.CultureInfo.InvariantCulture);

                string name = Regex.Replace(line, @"\d+[.,]\d{2}.*$", "").Trim();

                if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
                    continue;

                items.Add(new RItemModel
                {
                    Name = name,
                    Quantity = hasQuantity ? tempQty : 1,
                    UnitPrice = hasQuantity ? tempUnitPrice : totalPrice,
                    TotalPrice = totalPrice,
                    IsWeighted = hasQuantity
                });

                hasQuantity = false;
                tempQty = 1;
                tempUnitPrice = 0;
            }

            return items;
        }
    }
}
