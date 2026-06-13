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

        private static readonly string[] TotalKeywords =
        {
            "ОБЩО",
            "ОБЩА",
            "СУМА",
            "ЗА ПЛАЩАНЕ",
            "ДЪЛЖИМА",
            "ВСИЧКО"
        };

        private static readonly string[] EurKeywords =
        {
            "ЕВРО",
            "EUR",
            "EURO"
        };

        private static readonly string[] BgnKeywords =
        {
            "ЛВ",
            "ЛЕВА",
            "BGN",
            "ЛЕ"
        };

        private static readonly string[] TotalIgnoreKeywords =
        {
            "МЕЖДИННА",
            "ОТСТЪПКА",
            "ОТСТЪПКИ",
            "КУПОН",
            "ВАУЧЕР",
            "РЕСТО",
            "ДДС",
            "КАРТА",
            "КРЕДИТНА",
            "ДЕБИТНА"
        };

        private static readonly string[] InvalidProductKeywords =
        {
            "ОБЩО",
            "ОБЩА СУМА",
            "МЕЖДИННА СУМА",
            "ОТСТЪПКА",
            "ОТСТЪПКИ",
            "ЕВРО",
            "СУМА",
            "ДДС",
            "КАСОВ",
            "ФИСКАЛЕН",
            "БОН",
            "ПЛАЩАНЕ",
            "РЕСТО",
            "КАРТА",
            "В БРОЙ",
            "ОПЕРАТОР",
            "КАСИЕР"
        };

        private static readonly string[] ProductNoiseKeywords =
        {
            "ОТСТЪПКА",
            "ОТСТЪПКИ",
            "LIDL PLUS",
            "КУПОН",
            "КУПОНИ",
            "ВАУЧЕР"
        };

        private static readonly Regex QuantityRegex =
            new Regex(@"\d+(?:[.,]\d+)?\s*x\s*\d+[.,]\d{1,2}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PriceRegex =
            new Regex(@"(?<!\d)\d+[.,]\d{1,2}(?!\d)", RegexOptions.Compiled);

        public string? ExtractVendorLine(List<string> lines, int headerLinesCount)
        {
            var cleaned = lines
                .Where(l => !ContainsAny(l, IgnoreKeywordsVendor)) // Филтриране на редовете, които съдържат думи за игнориране
                .ToList();

            if (cleaned.Count == 0)
                return null;

            var legal = cleaned
                .FirstOrDefault(l => ContainsAny(l, LegalKeywordsVendor)); // Извлича първия ред, който съдържа данни за търговеца

            if (!string.IsNullOrWhiteSpace(legal))
                return legal;

            return null;
        }

        private string NormalizeOCRText(string text)
        {
            return text

                // Нов ред
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')

                // Символи за умножение
                .Replace("×", "x")
                .Replace("х", "x")
                .Replace("Х", "x")
                .Replace("*", "x")

                // Грешки при изписване на лева "лв"
                .Replace("ЛB", "ЛВ")
                .Replace("лB", "лв")
                .Replace("LB", "ЛВ")
                .Replace("IB", "ЛВ")
                .Replace("BG ", "BG")
                .Replace("B G", "BG")

                // Грешки при разделители
                .Replace("‚", ",")
                .Replace("‘", "'")
                .Replace("’", "'")
                .Replace("`", "'")
                .Replace("“", "\"")
                .Replace("”", "\"")

                // Ненужни интервали
                .Replace("  ", " ")
                .Replace("   ", " ")

                .Replace("|", "1")

                // Често срещани грешки между латиница и кирилица
                .Replace("K", "К")
                .Replace("M", "М")
                .Replace("H", "Н")
                .Replace("P", "Р")
                .Replace("C", "С")
                .Replace("X", "Х")
                .Replace("!", "l") //           !!!

                .Replace("\t", " ")
                .Replace("..", ".")
                .Replace(",.", ".")
                .Replace(".,", ".")
                .Replace("»", "")
                .Replace(">", "")
                .Replace("«", "")
                .Replace(" ,", ",")
                .Replace(", ", ",")
                .Replace(" .", ".")
                .Replace(". ", ".");
        }

        public List<string> GetCleanLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            text = NormalizeOCRText(text);

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

        public DateTime? ExtractDateTime(List<string> lines)
        {
            var regex = new Regex(@"\d{1,2}[./-]\d{1,2}[./-]\d{2,4}(\s+\d{1,2}:\d{2}(:\d{2})?)?");
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

        public decimal? ExtractTotalSumEUR(List<string> lines)
        {
            var totalLines = GetTotalLines(lines);

            foreach (var line in totalLines)
            {
                if (!ContainsAny(line, EurKeywords))
                    continue;

                var price = ExtractLastPrice(line);

                if (price.HasValue)
                    return price;
            }

            return null;
        }

        public decimal? ExtractTotalSumBGN(List<string> lines)
        {
            var totalLines = GetTotalLines(lines);

            foreach (var line in totalLines)
            {
                if (!ContainsAny(line, BgnKeywords))
                    continue;

                var price = ExtractLastPrice(line);

                if (price.HasValue)
                    return price;
            }

            return null;
        }

        private List<string> GetTotalLines(List<string> lines)
        {
            var result = new List<string>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (!ContainsAny(lines[i], TotalKeywords))
                    continue;

                if (ContainsAny(lines[i], TotalIgnoreKeywords))
                    continue;

                if (i > 0)
                    result.Add(lines[i - 1]);

                result.Add(lines[i]);

                if (i < lines.Count - 1)
                    result.Add(lines[i + 1]);
            }

            return result.Distinct().ToList();
        }

        private static decimal? ExtractLastPrice(string line)
        {
            var prices = PriceRegex.Matches(line)
                .Select(m => ParseDecimal(m.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (prices.Count == 0)
                return null;

            return prices[^1];
        }

        private static decimal? ParseDecimal(string value)
        {
            value = NormalizePriceValue(value);

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

        private static string NormalizePriceValue(string value)
        {
            value = value.Trim();

            var separatorIndex = Math.Max(value.LastIndexOf('.'), value.LastIndexOf(','));
            if (separatorIndex < 0)
                return value;

            var decimalPartLength = value.Length - separatorIndex - 1;
            if (decimalPartLength == 1)
                value += "0";

            return value;
        }

        private static bool IsValidProductLine(string line)
        {
            line = line.ToUpperInvariant();
         
            if (ContainsAny(line, InvalidProductKeywords))
                return false;

            return true;
        }
        private static bool IsTotalLine(string line)
        {
            return ContainsAny(line, TotalKeywords)
                || line.Contains("ЗА ПЛАЩАНЕ", StringComparison.OrdinalIgnoreCase)
                || line.Contains("ДЪЛЖИМА СУМА", StringComparison.OrdinalIgnoreCase)
                || line.Contains("ВСИЧКО", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProductName(string line)
        {
            if (line.Length < 2)
                return false;

            if (ContainsAny(line, InvalidProductKeywords))
                return false;

            if (PriceRegex.IsMatch(line))
                return false;

            if (!line.Any(char.IsLetter))
                return false;

            var lettersCount = line.Count(char.IsLetter);
            var digitsCount = line.Count(char.IsDigit);

            return lettersCount >= digitsCount;
        }

        private static bool IsProductNoiseLine(string line)
        {
            return ContainsAny(line, ProductNoiseKeywords)
                || line.StartsWith("#", StringComparison.Ordinal);
        }

        private static string GetCleanProductName(string line)
        {
            var name = PriceRegex.Replace(line, " ");
            name = Regex.Replace(name, @"\b\d+(?:[.,]\d+)?\s*x\s*\d+[.,]\d{1,2}\b", " ", RegexOptions.IgnoreCase);
            name = RemoveSuffix(name);
            name = Regex.Replace(name, @"(?<=\d)\s*96\b", "%");
            name = Regex.Replace(name, @"\s+", " ").Trim(' ', '-', ':', ';', '.', ',');

            return name;
        }

        private static List<decimal> ExtractPrices(string line)
        {
            return PriceRegex.Matches(line)
                .Select(m => ParseDecimal(m.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();
        }

        private static bool TryReadQuantityLine(string line, out decimal quantity, out decimal unitPrice)
        {
            quantity = 1;
            unitPrice = 0;

            if (!QuantityRegex.IsMatch(line))
                return false;

            var numbers = Regex.Matches(line, @"\d+(?:[.,]\d+)?")
                .Select(m => ParseDecimal(m.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (numbers.Count < 2)
                return false;

            quantity = numbers[0];
            unitPrice = numbers[1];
            return true;
        }

        private static string RemoveSuffix(string name)
        {
            return Regex.Replace(name, @"\s+[АБВГДABVCС6]$", "", RegexOptions.IgnoreCase);
        }

        private static int FindItemsStartIndex(List<string> lines, int totalIndex)
        {
            var maxHeaderSearch = Math.Min(totalIndex, 12);

            for (int i = 0; i < maxHeaderSearch; i++)
            {
                var line = lines[i];

                if (line.Contains("УНП", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("КАСА", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("КАСИЕР", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("ОПЕРАТОР", StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1;
                }
            }

            return 0;
        }

        public List<RItemModel> ExtractPurchases(List<string> lines)
        {
            int totalIndex = lines.FindIndex(IsTotalLine);
            if (totalIndex < 0)
                totalIndex = lines.Count;

            var items = new List<RItemModel>();
            var startIndex = FindItemsStartIndex(lines, totalIndex);

            string? currentName = null;
            decimal currentQuantity = 1;
            decimal currentUnitPrice = 0;
            bool hasQuantity = false;

            for (int i = startIndex; i < totalIndex; i++)
            {
                var line = lines[i];

                if (!IsValidProductLine(line))
                    continue;

                if (IsProductNoiseLine(line))
                    continue;

                if (TryReadQuantityLine(line, out var quantity, out var unitPrice))
                {
                    currentQuantity = quantity;
                    currentUnitPrice = unitPrice;
                    hasQuantity = true;
                    continue;
                }

                var prices = ExtractPrices(line);

                if (prices.Count == 0)
                {
                    if (IsProductName(line))
                        currentName = GetCleanProductName(line);

                    continue;
                }

                decimal totalPrice = prices[^1];

                string name = GetCleanProductName(line);

                if (string.IsNullOrWhiteSpace(name))
                    name = currentName ?? "";

                name = GetCleanProductName(name);

                if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
                {
                    currentName = null;
                    hasQuantity = false;
                    currentQuantity = 1;
                    currentUnitPrice = 0;
                    continue;
                }

                if (!IsValidProductLine(name))
                    continue;

                items.Add(new RItemModel
                {
                    Name = name,
                    Quantity = hasQuantity ? currentQuantity : 1,
                    UnitPrice = hasQuantity ? currentUnitPrice : totalPrice,
                    TotalPrice = totalPrice,
                    IsWeighted = hasQuantity
                });

                currentName = null;
                hasQuantity = false;
                currentQuantity = 1;
                currentUnitPrice = 0;
            }

            return items;
        }
    }
}
