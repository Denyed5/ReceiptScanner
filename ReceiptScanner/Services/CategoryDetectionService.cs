using Microsoft.EntityFrameworkCore;
using ReceiptScanner.Data;
using ReceiptScanner.Models;

namespace ReceiptScanner.Services
{
    public class CategoryDetectionResult
    {
        public string CategoryId { get; set; } = "16";

        public bool IsSuggested { get; set; }
    }

    public class CategoryDetectionService
    {
        private readonly ReceiptScannerContext _context;

        private readonly Dictionary<string, string[]> _categoryKeywords =
            new()
            {
                // 1 - Месо
                ["1"] = new[]
                {
                    "пилешко",
                    "пилешки",
                    "свинско",
                    "свински",
                    "телешко",
                    "телешки",
                    "кайма",
                    "салам",
                    "шунка",
                    "бекон",
                    "луканков",
                    "наденица",
                    "кебапче",
                    "кренвирш",
                    "сланина",
                    "пастет",
                    "пастърма"
                },

                // 2 - Напитки
                ["2"] = new[]
                {
                    "вода",
                    "cola",
                    "coca",
                    "derby",
                    "дерби",
                    "xixo",
                    "хiхo",
                    "fanta",
                    "sprite",
                    "бира",
                    "сок",
                    "кафе",
                    "чай",
                    "айрян",
                    "безалкохолно",
                    "red bull"
                },

                // 3 - Плодове
                ["3"] = new[]
                {
                    "ябълка", "ябълки", "банан", "банани",
                    "портокал", "портокали", "лимон", "лимони",
                    "грозде", "праскова", "праскови", "ягода",
                    "ягоди", "киви", "диня", "пъпеш",
                    "мандарина", "мандарини", "череша", "череши",
                    "вишна", "вишни", "кайсия", "кайсии",
                    "слива", "сливи", "смокиня", "смокини",
                    "круша", "круши", "ананас", "манго",
                    "авокадо", "кокос", "боровинка", "боровинки",
                    "малина", "малини", "къпина", "къпини",
                    "нар", "помело", "лайм"
                },

                // 4 - Зеленчуци
                ["4"] = new[]
                {
                    "домати",
                    "краставици",
                    "картофи",
                    "лук",
                    "морков",
                    "чушка",
                    "зеле",
                    "маруля",
                    "корнишони"
                },

                // 5 - Млечни
                ["5"] = new[]
                {
                    "мляко",
                    "кисело",
                    "сирене",
                    "кашкавал",
                    "масло",
                    "извара",
                    "сметана"
                },

                // 6 - Хляб и тестени
                ["6"] = new[]
                {
                    "хляб",
                    "питка",
                    "баничка",
                    "кроасан",
                    "спагети",
                    "макарони",
                    "тесто"
                },

                // 7 - Замразени храни
                ["7"] = new[]
                {
                    "замраз",
                    "фри",
                    "сладолед"
                },

                // 8 - Сладки и десерти
                ["8"] = new[]
                {
                    "шоколад",
                    "бонбони",
                    "вафла",
                    "бисквити",
                    "поничка",
                    "пудинг",
                    "десерт",
                    "кекс",
                    "сусамк",
                    "донът"
                },

                // 9 - Снаксове
                ["9"] = new[]
                {
                    "чипс",
                    "солети",
                    "крекер",
                    "пуканки",
                    "ядки"
                },

                // 10 - Консерви
                ["10"] = new[]
                {
                    "консерва",
                    "лютеница",
                    "буркан",
                    "риба",
                    "тон"
                },

                // 11 - Подправки и сосове
                ["11"] = new[]
                {
                    "сол",
                    "пипер",
                    "кетчуп",
                    "майонеза",
                    "горчица",
                    "сос",
                    "подправка"
                },

                // 12 - Готови храни
                ["12"] = new[]
                {
                    "сандвич",
                    "дюнер",
                    "бургер",
                    "суши",
                    "салата",
                    "пица",
                    "онигири",
                    "закуска",
                    "ястие"
                },

                // 13 - Домашни потреби
                ["13"] = new[]
                {
                    "торбичка",
                    "торба",
                    "салфетки",
                    "гъба",
                    "препарат",
                    "веро"
                },

                // 14 - Козметика и хигиена
                ["14"] = new[]
                {
                    "шампоан",
                    "паста",
                    "четка",
                    "сапун",
                    "дезодорант",
                    "тоалетна"
                },

                // 15 - Алкохол
                ["15"] = new[]
                {
                    "вино",
                    "ракия",
                    "уиски",
                    "водка",
                    "джин",
                    "ром"
                }
            };

        public CategoryDetectionService(
            ReceiptScannerContext context)
        {
            _context = context;
        }

        public async Task<CategoryDetectionResult>
            DetectCategoryAsync(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return new CategoryDetectionResult
                {
                    CategoryId = "16",
                    IsSuggested = false
                };
            }

            var normalized =
                productName.ToLowerInvariant();

            foreach (var category in _categoryKeywords)
            {
                if (category.Value.Any(keyword =>
                    normalized.Contains(keyword)))
                {
                    return new CategoryDetectionResult
                    {
                        CategoryId = category.Key,
                        IsSuggested = false
                    };
                }
            }

            foreach (var category in _categoryKeywords)
            {
                foreach (var keyword in category.Value)
                {
                    var words =
                        normalized.Split(
                            ' ',
                            StringSplitOptions.RemoveEmptyEntries);

                    foreach (var word in words)
                    {
                        int distance =
                            LevenshteinDistance(
                                word,
                                keyword);

                        if (distance <= 1)
                        {
                            return new CategoryDetectionResult
                            {
                                CategoryId = category.Key,
                                IsSuggested = true
                            };
                        }
                    }
                }
            }

            return new CategoryDetectionResult
            {
                CategoryId = "16",
                IsSuggested = false
            };
        }

        private static int LevenshteinDistance(
            string source,
            string target)  
        {
            int[,] matrix =
                new int[source.Length + 1,
                        target.Length + 1];

            for (int i = 0; i <= source.Length; i++)
            {
                matrix[i, 0] = i;
            }

            for (int j = 0; j <= target.Length; j++)
            {
                matrix[0, j] = j;
            }

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost =
                        source[i - 1] == target[j - 1]
                        ? 0
                        : 1;

                    matrix[i, j] =
                        Math.Min(
                            Math.Min(
                                matrix[i - 1, j] + 1,
                                matrix[i, j - 1] + 1),
                            matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[source.Length, target.Length];
        }
    }
}