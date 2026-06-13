using OpenCvSharp;

namespace ReceiptScanner.Services
{
    public class ImagePreprocessingService
    {
        private const int MinOcrWidth = 1400;
        private const int MaxOcrWidth = 2600;
        private const double MinDeskewAngle = 0.7;
        private const double MaxDeskewAngle = 8.0;

        public Mat Preprocess(Mat input)
        {
            using Mat resized = ResizeForOcr(input);
            using Mat gray = ConvertToGray(resized);
            using Mat normalized = NormalizeIllumination(gray);
            using Mat denoised = Denoise(normalized);
            using Mat deskewed = DeskewIfConfident(denoised);

            return Sharpen(deskewed);
        }

        internal Mat ResizeForOcr(Mat image)
        {
            int targetWidth = image.Width;

            if (image.Width < MinOcrWidth)
            {
                targetWidth = MinOcrWidth;
            }
            else if (image.Width > MaxOcrWidth)
            {
                targetWidth = MaxOcrWidth;
            }
            else
            {
                return image.Clone();
            }

            double ratio = targetWidth / (double)image.Width;
            int newHeight = (int)(image.Height * ratio);

            Mat resized = new Mat();
            var interpolation = ratio > 1
                ? InterpolationFlags.Cubic
                : InterpolationFlags.Area;

            Cv2.Resize(image, resized, new Size(targetWidth, newHeight), 0, 0, interpolation);

            return resized;
        }

        internal Mat ConvertToGray(Mat image)
        {
            if (image.Channels() == 1)
            {
                return image.Clone();
            }

            Mat gray = new Mat();

            var conversion = image.Channels() == 4
                ? ColorConversionCodes.BGRA2GRAY
                : ColorConversionCodes.BGR2GRAY;

            Cv2.CvtColor(image, gray, conversion);
            return gray;
        }

        private Mat NormalizeIllumination(Mat image)
        {
            using var clahe = Cv2.CreateCLAHE(2.0, new Size(8, 8));
            Mat enhanced = new Mat();
            clahe.Apply(image, enhanced);
            return enhanced;
        }

        private Mat Denoise(Mat image)
        {
            Mat denoised = new Mat();
            Cv2.FastNlMeansDenoising(image, denoised, 7, 7, 21);
            return denoised;
        }

        private Mat DeskewIfConfident(Mat image)
        {
            using Mat threshold = new Mat();
            Cv2.Threshold(image, threshold, 0, 255,
                ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(30, 3));
            Cv2.MorphologyEx(threshold, threshold, MorphTypes.Close, kernel);

            Point[][] contours;
            HierarchyIndex[] hierarchy;

            Cv2.FindContours(threshold, out contours, out hierarchy,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            var angles = new List<double>();

            foreach (var contour in contours)
            {
                var rect = Cv2.MinAreaRect(contour);
                var area = rect.Size.Width * rect.Size.Height;

                if (area < 250 || rect.Size.Width < 25 || rect.Size.Height < 4)
                {
                    continue;
                }

                var angle = rect.Angle;

                if (angle < -45)
                {
                    angle += 90;
                }

                if (Math.Abs(angle) <= MaxDeskewAngle)
                {
                    angles.Add(angle);
                }
            }

            if (angles.Count < 5)
            {
                return image.Clone();
            }

            angles.Sort();
            var medianAngle = angles[angles.Count / 2];

            if (Math.Abs(medianAngle) < MinDeskewAngle)
            {
                return image.Clone();
            }

            using Mat rotationMatrix = Cv2.GetRotationMatrix2D(
                new Point2f(image.Width / 2f, image.Height / 2f),
                medianAngle,
                1.0);

            Mat rotated = new Mat();
            Cv2.WarpAffine(image, rotated, rotationMatrix, image.Size(), InterpolationFlags.Linear, BorderTypes.Replicate);
            return rotated;
        }

        private Mat Sharpen(Mat image)
        {
            using Mat blurred = new Mat();
            Cv2.GaussianBlur(image, blurred, new Size(0, 0), 1.0);

            Mat sharpened = new Mat();
            Cv2.AddWeighted(image, 1.45, blurred, -0.45, 0, sharpened);
            return sharpened;
        }
    }
}
