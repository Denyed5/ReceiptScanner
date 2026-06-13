using System;
using System.Collections.Generic;
using System.Text;
using ReceiptScanner.Services;

namespace ReceiptScanner.Tests
{
    public class ReceiptParserServiceTests
    {
        public readonly ReceiptParserService _service;

        public ReceiptParserServiceTests()
        {
            _service = new ReceiptParserService();
        }

        //Тестът проверява дали функцията правилно изчиства подадения й текст
        //от изличните празни места и редове
        [Fact]
        public void GetCleanLines_InputEmptyLines_ReturnsCleanedList()
        {
            // Arrange
            var text = "   Ред 1   \n   \n   \n   Ред 2";

            // Act
            var result = _service.GetCleanLines(text);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Ред 1", result[0]);
            Assert.Equal("Ред 2", result[1]);
        }

        //Тестът проверява дали функцията правилно определя реда с името на търговеца
        [Fact]
        public void ExtractVendorLine_InputExampleOcrText_ReturnsVendorName() {
            var lines = new List<string> 
                {"ФИСКАЛЕН БОН",
                 "ЛИДЛ БЪЛГАРИЯ ЕООД",
                 "БУЛСТАТ 123456"};

            var result = _service.ExtractVendorLine(lines, 3);

            Assert.Equal("ЛИДЛ БЪЛГАРИЯ ЕООД", result);
        }

        //Тестът проверява дали функцията връща правилен отговор (Null),
        //когато в подадения й текст не се съдържа име на търговец
        [Fact]
        public void ExtractVendorLine_InputTextWithoutVendor_ReturnsNull()
        {
            var lines = new List<string>
                {"ФИСКАЛЕН БОН",
                 "БУЛСТАТ 123456"};

            var result = _service.ExtractVendorLine(lines, 2);

            Assert.Null(result);
        }

        //Тестът проверява дали функцията правилно извлича датата при различните формати
        [Theory]
        [InlineData("Дата: 15.06.2026", 2026, 6, 15)]
        [InlineData("Дата: 15/06/2026", 2026, 6, 15)]
        [InlineData("Дата: 15-06-2026", 2026, 6, 15)]
        [InlineData("15.06.26", 2026, 6, 15)]
        [InlineData("15/06/26", 2026, 6, 15)]
        public void ExtractDateTime_InputValidDateFormats_ReturnsCorrectDateExtracted(string line, int year, int month, int day)
        {
            // Arrange
            var lines = new List<string> { line };

            // Act
            var result = _service.ExtractDateTime(lines);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(year, result.Value.Year);
            Assert.Equal(month, result.Value.Month);
            Assert.Equal(day, result.Value.Day);
        }

        //Тестът проверява дали функцията намира реда с датата и часа
        //,когато има ключовата дума "Дата" и когато липсва
        [Fact]
        public void ExtractDateTime_InputMultipleLinesWithDate_ReturnsCorrectDateLine()
        {
            var lines = new List<string>
                {"ЛИДЛ БЪЛГАРИЯ ЕООД",
                 "ФИСКАЛЕН БОН",
                 "Дата: 15.06.2026 13:45:35",
                 "ОБЩО 12.50"};

            var lines2 = new List<string>
                {"ЛИДЛ БЪЛГАРИЯ ЕООД",
                 "ФИСКАЛЕН БОН",
                 "15.06.2026 13:45:35",
                 "ОБЩО 12.50"};

            var result = _service.ExtractDateTime(lines);
            var result2 = _service.ExtractDateTime(lines2);

            Assert.Equal(new DateTime(2026, 6, 15, 13, 45, 35), result);
            Assert.Equal(new DateTime(2026, 6, 15, 13, 45, 35), result2);
        }

        //Тестът проверява дали функцията правилно определя и извлича
        //реда с общата сума в лева
        [Fact]
        public void ExtractTotalSumBGN_InputExampleTextWithTotal_ReturnsCorrectTotalInBGN() {
            var lines = new List<string>
                        {"Мляко 2.50",
                         "Хляб 1.80",
                         "ОБЩО ЛВ 4.30",
                         "ОБЩО В ЕВРО 2.20"};

            var result = _service.ExtractTotalSumBGN(lines);

            Assert.Equal(4.30m, result);
        }

        //Тестът проверява дали функцията правилно определя и извлича
        //реда с общата сума в евро
        [Fact]
        public void ExtractTotalSumEUR_InputExampleTextWithTotal_ReturnsCorrectTotalInEUR()
        {
            var lines = new List<string>
                        {"Мляко 2.50",
                         "Хляб 1.80",
                         "ОБЩО ЛВ 4.30",
                         "ОБЩО В ЕВРО 2.20"};

            var result = _service.ExtractTotalSumEUR(lines);

            Assert.Equal(2.20m, result);
        }


        //Тестът проверява дали функцията извлича правилно продукт от ред 
        [Fact]
        public void ExtractPurchases_InputSingleProduct_ReturnsCorrectProductLine()
        {
            var lines = new List<string>
            {
                "МЛЯКО 2.50",
                "ОБЩО ЛВ 2.50"
            };

            var result = _service.ExtractPurchases(lines);

            Assert.Single(result);

            Assert.Equal("МЛЯКО", result[0].Name);
            Assert.Equal(2.50m, result[0].TotalPrice);
        }

        //Тестът проверява дали функцията успява да извлече множество продукти
        [Fact]
        public void ExtractPurchases_InputMultipleProducts_ReturnsAllProductLines()
        {
            var lines = new List<string>
            {
                "МЛЯКО 2.50",
                "ХЛЯБ 1.80",
                "ОБЩО ЛВ 4.30"
            };

            var result = _service.ExtractPurchases(lines);

            Assert.Equal(2, result.Count);
        }


        //Тестът проверява дали функцията за извличане на количество на продукт работи коректно
        [Fact]
        public void ExtractPurchases_WeightedProduct_ReturnsCorrectQuantity()
        {

            var lines = new List<string>
            {
                "1.250 x 2.99",
                "ЯБЪЛКИ  3.74 Б"
            };

            var result = _service.ExtractPurchases(lines);

            Assert.Single(result);

            Assert.Equal("ЯБЪЛКИ", result[0].Name);
            Assert.Equal(1.250m, result[0].Quantity);
            Assert.Equal(2.99m, result[0].UnitPrice);
            Assert.Equal(3.74m, result[0].TotalPrice);
        }


        //Тестът проверява дали функцията ограничава правилно списъка със стоки
        [Fact]
        public void ExtractPurchases_InputTextWithProducts_ReturnsCorrectProductInformation()
        {

            var lines = new List<string>
                {
                "ЛИДЛ БЪЛГАРИЯ ЕООД",
                "КАСИЕР: 12345",
                "1.250 x 2.99",
                "ЯБЪЛКИ 3.74 Б",
                "2.32 x 1.99",
                "Bananа 4.62 Б",
                "ОБЩО ЛВ 8.36",
                "Купи салам за 1.20",
                "и получи втори безплатно"
                };

            var result = _service.ExtractPurchases(lines);

            Assert.Equal(2, result.Count); // Извлечени за точно 2 продукта
                                           // (проверява дали рекламата накрая не се засича по погрешка)
            Assert.Equal("ЯБЪЛКИ", result[0].Name);
            Assert.Equal(1.250m, result[0].Quantity);
            Assert.Equal(2.99m, result[0].UnitPrice);
            Assert.Equal(3.74m, result[0].TotalPrice);

            Assert.Equal("Bananа", result[1].Name);
            Assert.Equal(2.32m, result[1].Quantity);
            Assert.Equal(1.99m, result[1].UnitPrice);
            Assert.Equal(4.62m, result[1].TotalPrice);
        }
    }
}
