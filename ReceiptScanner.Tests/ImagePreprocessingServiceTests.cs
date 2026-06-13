using Moq;
using OpenCvSharp;
using ReceiptScanner.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReceiptScanner.Tests
{
    public class ImagePreprocessingServiceTests
    {
        private readonly ImagePreprocessingService _ipService;

        public ImagePreprocessingServiceTests()
        {
            _ipService = new ImagePreprocessingService();
        }

        //Тестът проверява дали при подадено изображение с ширина под минималните 1400 пиксела (1000x1000p),
        //функцията връща изображение с минималната зададена ширина 1400 пиксела, като запазва и пропорциите
        [Fact]
        public void ResizeForOcr_InputWidthBelowMin_ReturnsUpscaledImage()
        {
            // Arrange
            var image = new Mat(1000, 1000, MatType.CV_8UC1);

            // Act
            var result = _ipService.ResizeForOcr(image);

            // Assert
            Assert.Equal(1400, result.Width);
            Assert.Equal(1400, result.Height);
        }

        //Тестът проверява дали при подадено входно изображение с висока ширина (над 2500 пиксела),
        //функцията намалява размера на изображението като запазва пропорциите.
        [Fact]
        public void ResizeForOcr_InputWidthAboveMax_ReturnsDownscaledImage()
        {
            // Arrange
            var image = new Mat(1500, 3000, MatType.CV_8UC1);

            // Act
            var result = _ipService.ResizeForOcr(image);

            // Assert
            Assert.Equal(2600, result.Width);

            var expectedHeight = (int)(1500 * (2600.0 / 3000.0));
            Assert.Equal(expectedHeight, result.Height);
        }

        //Тестът проверява дали при подаване на изображение с 3 цветови канала,
        //функцията връща изображение в 1 канал - изображение в нива на сивото
        [Fact]
        public void ConvertToGray_InputImage3ColorChannels_ReturnsImageTo1ColorChannel(){
            // Arrange
            var image = new Mat(100, 100, MatType.CV_8UC3);

            // Act
            var result = _ipService.ConvertToGray(image);

            // Assert
            Assert.Equal(1, result.Channels());
        }

        //Тестът проверява дали при подаване на изображение с 4 цветови канала,
        //функцията връща изображение в 1 канал - изображение в нива на сивото
        [Fact]
        public void ConvertToGray_InputImage4ColorChannels_ReturnsImageTo1ColorChannel()
        {
            // Arrange
            var image = new Mat(100, 100, MatType.CV_8UC4);

            // Act
            var result = _ipService.ConvertToGray(image);

            // Assert
            Assert.Equal(1, result.Channels());
        }
    }
}
