using ReceiptScanner.Models;
using System.Text.RegularExpressions;

namespace ReceiptScanner.Services
{
    public class ReceiptParserService
    {
        // ------------------------------- Извличане на търговец -------------------------------- //

        // Търсени думи
        private static readonly string[] LegalKeywordsVendor =
        {   "ЕООД", "ООД", "АД",
            "ЕТ", "КД", "Пацони",
            "Кауфланд", "Лидл" };

        // Избягвани думи
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

        private static readonly Regex PriceRegex =
            new Regex(@"\d+[.,]\d{2}\s*", RegexOptions.Compiled);

        private static readonly Regex QuantityRegex =
            new Regex(@"\d+[.,]\d+\s*x\s*\d+[.,]\d+", RegexOptions.Compiled);


        // Извлича реда с името на търговеца
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

            return "Не може да бъде извлечен.";
        }

        private static List<string> GetCleanLines(string text)
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

        // Функция за проверка дали даден ред съдържа някоя от търсените ключови думи
        private static bool ContainsAny(string line, string[] keywords)
        {
            foreach (var k in keywords)
            {
                if (line.Contains(k, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // ------------------------------- Извличане на датата -------------------------------- //

        public string? ExtractDateTime(string rawText)
        {
            var regex = new Regex(@"\d{1,2}[./-]\d{1,2}[./-]\d{2,4}(\s+\d{1,2}:\d{2})?|\d{4}[./-]\d{1,2}[./-]\d{1,2}(\s+\d{1,2}:\d{2})?"); // regex за формата ДД.ММ.ГГГГ, ДД/ММ/ГГГГ, ДД-ММ-ГГГГ, ГГГГ.ММ.ДД, ГГГГ/ММ/ДД, ГГГГ-ММ-ДД
            var lines = GetCleanLines(rawText);
            string result;

            foreach (var line in lines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    return result = match.ToString();
                }
            }

            return "Не може да бъде извлечена.";
        }


        // ------------------------------- Извличане на общата сума -------------------------------- //

        public string? ExtractTotalSum(string rawText)
        {
            var lines = GetCleanLines(rawText);

            var result = lines
                .FirstOrDefault(l => ContainsAny(l, LegalKeywordsTotal));

            if (!string.IsNullOrWhiteSpace(result))
                return result;


            return "Не може да бъде извлечена.";
        }

        // ------------------------------- Извличане на покупки -------------------------------- //

        public List<ReceiptItem> ExtractPurchases(string rawText)
        {
            var lines = GetCleanLines(rawText);

            int totalIndex = lines.FindIndex(l =>
                ContainsAny(l, LegalKeywordsTotal));

            if (totalIndex < 0)
                return new List<ReceiptItem>();

            int startIndex = lines.FindIndex(l =>
                PriceRegex.IsMatch(l) &&
                l.Any(char.IsLetter));

            if (startIndex < 0 || startIndex >= totalIndex)
                return new List<ReceiptItem>();

            var items = new List<ReceiptItem>();

            decimal tempQty = 1;
            decimal tempUnitPrice = 0;
            bool hasQuantity = false;

            for (int i = startIndex; i < totalIndex; i++)
            {
                var line = lines[i];

                // Quantity line
                if (QuantityRegex.IsMatch(line))
                {
                    var numbers = Regex.Matches(line, @"\d+[.,]\d+")
                                       .Select(m => decimal.Parse(
                                           m.Value.Replace(',', '.')))
                                       .ToList();

                    if (numbers.Count == 2)
                    {
                        tempQty = numbers[0];
                        tempUnitPrice = numbers[1];
                        hasQuantity = true;
                    }

                    continue;
                }

                // Product line
                if (PriceRegex.IsMatch(line))
                {
                    var match = PriceRegex.Match(line);

                    decimal totalPrice = decimal.Parse(
                        match.Value.Replace(',', '.'));

                    string name = line.Substring(0, match.Index).Trim();

                    items.Add(new ReceiptItem
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
            }
            return items;
        }
    }
}
