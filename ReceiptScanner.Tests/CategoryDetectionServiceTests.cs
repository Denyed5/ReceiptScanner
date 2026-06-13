using ReceiptScanner.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ReceiptScanner.Tests
{
    public class CategoryDetectionServiceTests
    {
        private readonly CategoryDetectionService _service;

        public CategoryDetectionServiceTests() { 
        _service = new CategoryDetectionService();
        }


        //Проверява дали при подаден празен низ за име на продукта, (въпреки, че това се избягва в ReceiptParserService.cs и OcrService.cs)
        //функцията връща резултат категория други (id = 16).
            [Fact]
        public async Task DetectCategory_InputEmpty_ReturnsOthersCategory()
        {
            // Arrange
            string productName = "";

            // Act
            var result = await _service.DetectCategory(productName);

            // Assert
            Assert.Equal("16", result.CategoryId);
            Assert.False(result.IsSuggested);
        }

        //Тестът проверява дали при подаване на перфектно изписано име на продукт, той се засича от функцията
        //и се определя правилно категорията, в случая млечни продукти
        [Fact]
        public async Task DetectCategory_InputPerfectName_ReturnsCorrectCategory() {
            // Arrange
            string productName = "Прясно мляко";

            // Act
            var result = await _service.DetectCategory(productName);

            // Assert
            Assert.Equal("5", result.CategoryId);
            Assert.False(result.IsSuggested);
        }

        //Тестът проверява дали програмата правилно определя категорията при продукти изписани
        //с малки и големи букви
        [Fact]
        public async Task DetectCategory_InputWordsWithDifferentCases_ReturnsCorrectCategoryIgnoringCase()
        {
            // Arrange
            string productName = "мляко";
            string productName2 = "МляКо";
            string productName3 = "МЛЯКО";
            string productName4 = "МЛЯко";

            // Act
            var result = await _service.DetectCategory(productName);
            var result2 = await _service.DetectCategory(productName2);
            var result3 = await _service.DetectCategory(productName3);
            var result4 = await _service.DetectCategory(productName4);

            // Assert
            Assert.Equal("5", result.CategoryId);
            Assert.False(result.IsSuggested);
            Assert.Equal("5", result2.CategoryId);
            Assert.False(result2.IsSuggested);
            Assert.Equal("5", result3.CategoryId);
            Assert.False(result3.IsSuggested);
            Assert.Equal("5", result4.CategoryId);
            Assert.False(result4.IsSuggested);
        }

        //Тестът проверява дали алгоритъма на Левенщайн работи при определената позволена грешка от 1 символ
        //Функцията трябва да върне предполагаема категория
        [Fact]
        public async Task DetectCategory_InputWordWith1Error_ReturnsCorrectCategoryRecommendation()
        {
            // Arrange
            string productName = "млякс"; //мляко

            // Act
            var result = await _service.DetectCategory(productName);

            // Assert
            Assert.Equal("5", result.CategoryId);
            Assert.True(result.IsSuggested);
        }

        //Тестът проверява дали при подаване на неизвестни за системата входни данни,
        //функцията правилно определя категорията на продукта (категория други)
        [Fact]
        public async Task DetectCategory_InputUnknownProduct_ReturnsOthersCategory() 
        {
            // Arrange
            string productName = "лаптоп";

            // Act
            var result = await _service.DetectCategory(productName);

            // Assert
            Assert.Equal("16", result.CategoryId);
            Assert.False(result.IsSuggested);
        }

        //Тестът проверява дали при подаване на входни данни системата определя правилно категориите на продуктите
        [Theory]
        [InlineData("Пилешко филе", "1")]
        [InlineData("Минерална вода", "2")]
        [InlineData("Банани", "3")]
        [InlineData("Домати", "4")]
        [InlineData("Кисело мляко", "5")]
        [InlineData("Хляб Добруджа", "6")]
        [InlineData("Сладолед", "7")]
        [InlineData("Шоколад", "8")]
        [InlineData("Чипс", "9")]
        [InlineData("Лютеница", "10")]
        [InlineData("Кетчуп", "11")]
        [InlineData("Пица", "12")]
        [InlineData("Салфетки", "13")]
        [InlineData("Шампоан", "14")]
        [InlineData("Уиски", "15")]
        public async Task DetectCategoryAsync_ReturnsExpectedCategory(string productName, string expectedCategoryId)
        {
            // Act
            var result = await _service.DetectCategory(productName);

            // Assert
            Assert.Equal(expectedCategoryId, result.CategoryId);
            Assert.False(result.IsSuggested);
        }
    }
}
