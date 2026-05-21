using OpenCvSharp;

namespace ReceiptScanner.Services
{
    public class ImagePreprocessingService
    {

        public Mat Preprocess(Mat input)
        {
            string debugPath = Path.Combine("debug");
            Directory.CreateDirectory(debugPath);

            Mat resized = Resize(input);
            Cv2.ImWrite($"{debugPath}/1_resized.png", resized);

            Mat gray = ConvertToGray(resized);
            Cv2.ImWrite($"{debugPath}/2_gray.png", gray);

            Mat blurred = Blur(gray);
            Cv2.ImWrite($"{debugPath}/2_5_blur.png", blurred);

            Mat deskewed = Deskew(blurred);
            Cv2.ImWrite($"{debugPath}/3_deskew.png", deskewed);

            Cv2.MedianBlur(deskewed, deskewed, 3);

            Mat thresholded = ApplyAdaptiveThreshold(deskewed);
            Cv2.ImWrite($"{debugPath}/4_threshold.png", thresholded);

            Mat cleaned = CleanNoise(thresholded);
            Cv2.ImWrite($"{debugPath}/5_clean.png", cleaned);

            return cleaned;
        }

        private Mat Resize(Mat image)
        {
            int targetWidth = 1000;

            double ratio = (double)targetWidth / image.Width;
            int newHeight = (int)(image.Height * ratio);

            Mat resized = new Mat();
            Cv2.Resize(image, resized, new Size(targetWidth, newHeight));

            return resized;
        }

        private Mat ConvertToGray(Mat image)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            return gray;
        }

        private Mat Blur(Mat image)
        {
            Mat blur = new Mat();
            Cv2.GaussianBlur(image, blur, new Size(5, 5), 0);
            return blur;
        }

        private Mat ApplyAdaptiveThreshold(Mat image)
        {
            Mat thresh = new Mat();

            Cv2.AdaptiveThreshold(
                image,
                thresh,
                255,
                AdaptiveThresholdTypes.GaussianC,
                ThresholdTypes.Binary,
                11,
                2
            );

            return thresh;
        }

        private Mat Deskew(Mat image)
        {
            Mat gray = new Mat();

            if (image.Channels() > 1)
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            else
                gray = image.Clone();

            Mat thresh = new Mat();

            Cv2.Threshold(gray, thresh, 0, 255,
                ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
            Cv2.MorphologyEx(thresh, thresh, MorphTypes.Close, kernel);

            Point[][] contours;
            HierarchyIndex[] hierarchy;

            Cv2.FindContours(thresh, out contours, out hierarchy,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            double maxArea = 0;
            Point[]? largest = null;

            foreach (var cnt in contours)
            {
                double area = Cv2.ContourArea(cnt);
                if (area > maxArea)
                {
                    maxArea = area;
                    largest = cnt;
                }
            }

            if (largest == null)
                return image;

            RotatedRect box = Cv2.MinAreaRect(largest);

            double angle = box.Angle;

            if (angle < -45)
                angle += 90;

            Point2f center = new Point2f(image.Width / 2f, image.Height / 2f);

            Mat rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);

            Mat rotated = new Mat();

            Cv2.WarpAffine(image, rotated, rotationMatrix, image.Size(),
                InterpolationFlags.Cubic,
                BorderTypes.Replicate);

            return rotated;
        }

        private Mat CleanNoise(Mat image)
        {
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2));

            Mat cleaned = new Mat();

            Cv2.MorphologyEx(
                image,
                cleaned,
                MorphTypes.Close,
                kernel
            );

            return cleaned;
        }

    }
}