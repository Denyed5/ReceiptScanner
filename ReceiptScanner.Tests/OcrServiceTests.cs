using Moq;
using ReceiptScanner.Services;
using Microsoft.AspNetCore.Hosting;

namespace ReceiptScanner.Tests
{
    public class OcrServiceTests
    {
        private readonly OcrService _ocrService;

        public OcrServiceTests()
        {
            var envMock = new Mock<IWebHostEnvironment>();

            _ocrService = new OcrService(envMock.Object);
        }


        //Тестът проверява дали при стойност на входните данни null,
        //функцията връща правилно списък с празен низ.
        [Fact]
        public void CleanGarbage_InputNull_ReturnsEmptyString()
        {
            //Arrange
            string input = null;


            //Act
            var result = _ocrService.CleanGarbage(input);

            //Assert
            Assert.Equal("", result);
        }


        //Тестът проверява дали функцията изчиства правилно единични или двойка символи
        //като запазва структурата на текста в редове.
        [Fact]
        public void CleanGarbage_InputMixedLengthLines_ReturnsLinesLongerThan3Chars()
        {
            //Arrange
            string input = "а\nба\nМляко\nас\nХляб\nвас";


            //Act
            var result = _ocrService.CleanGarbage(input);

            //Assert
            Assert.Equal("Мляко\nХляб\nвас", result);
        }

        //Тестът проверява дали функцията премахва редовете, които съдържат само излишни символи,
        //но запазва редовете, които могат да съдържат информация. 
        [Fact]
        public void CleanGarbage_InputSymbolLines_ReturnsLinesWithLettersOrDigits()
        {
            //Arrange
            string input = "*****\n??23??1\nМляко ?@ 2.50\n..";

            //Act
            var result = _ocrService.CleanGarbage(input);

            //Assert
            Assert.Equal("??23??1\nМляко ?@ 2.50",result);
        }

        //Тестът проверява дали функцията правилно премахва излишните празни места и редове
        [Fact]
        public void CleanGarbage_InputWhiteSpacesWithText_ReturnsTextWithoutWhiteSpaces()
        {
            //Arrange
            string input = "  \n   \n      Мляко 2.50     ";

            //Act
            var result = _ocrService.CleanGarbage(input);

            //Assert
            Assert.Equal("Мляко 2.50", result);

        }

    }
    
}
