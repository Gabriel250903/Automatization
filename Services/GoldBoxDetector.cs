using Automatization.Types;
using System.Drawing.Imaging;

namespace Automatization.Services
{
    public class GoldBoxDetector(TextDetectorService textDetectorService)
    {
        private const int Tolerance = 15;
        private const int MinPixelCount = 50;
        private const string GoldBoxNotificationText = "Gold Box will be dropped soon";
        public Color TargetColor { get; set; } = ColorTranslator.FromHtml("#F69001");
        private readonly TextDetectorService _textDetectorService = textDetectorService;

        public unsafe bool Detect(Bitmap bmp)
        {
            DetectionResultStruct result = DetectWithDetails(bmp);
            return result.Success;
        }

        public unsafe DetectionResultStruct DetectWithDetails(Bitmap bmp)
        {
            DetectionResultStruct result = new();
            int width = bmp.Width;
            int height = bmp.Height;

            int scanStartY = 0;
            int scanEndY = (int)(height * 0.35);
            int scanStartX = (int)(width * 0.2);
            int scanEndX = (int)(width * 0.8);

            bool colorDetected = false;

            int minX = width, maxX = 0;
            int minY = height, maxY = 0;

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                byte* ptr = (byte*)data.Scan0;
                int stride = data.Stride;
                int matchCount = 0;

                int stepY = 2;
                int stepX = 2;

                for (int y = scanStartY; y < scanEndY; y += stepY)
                {
                    byte* row = ptr + (y * stride);

                    for (int x = scanStartX; x < scanEndX; x += stepX)
                    {
                        int b = row[x * 3];
                        int g = row[(x * 3) + 1];
                        int r = row[(x * 3) + 2];

                        if (IsColorMatch(r, g, b, TargetColor))
                        {
                            matchCount++;

                            if (x < minX)
                            {
                                minX = x;
                            }

                            if (x > maxX)
                            {
                                maxX = x;
                            }

                            if (y < minY)
                            {
                                minY = y;
                            }

                            if (y > maxY)
                            {
                                maxY = y;
                            }
                        }
                    }
                }

                if (matchCount > MinPixelCount)
                {
                    colorDetected = true;
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            result.ColorDetected = colorDetected;

            if (colorDetected)
            {
                int padding = 10;
                int cropX = Math.Max(0, minX - padding);
                int cropY = Math.Max(0, minY - padding);
                int cropWidth = Math.Min(width - cropX, maxX - minX + (padding * 2));
                int cropHeight = Math.Min(height - cropY, maxY - minY + (padding * 2));

                if (cropWidth <= 0 || cropHeight <= 0)
                {
                    result.Error = "Calculated crop region is invalid.";
                    return result;
                }

                Rectangle ocrRegion = new(cropX, cropY, cropWidth, cropHeight);

                using Bitmap ocrBmp = bmp.Clone(ocrRegion, bmp.PixelFormat);

                //try
                //{
                //    string debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ocr_debug.png");
                //    ocrBmp.Save(debugPath, ImageFormat.Png);
                //}
                //catch (Exception ex)
                //{
                //    LogService.LogError($"Failed to save OCR debug image: {ex.Message}");
                //}

                string? detectedText = _textDetectorService.GetTextFromBitmap(ocrBmp);
                result.DetectedText = detectedText;

                if (!string.IsNullOrEmpty(detectedText))
                {
                    int goldIndex = detectedText.IndexOf("Gold", StringComparison.OrdinalIgnoreCase);

                    if (goldIndex is >= 0 and < 10)
                    {
                        result.Success = true;
                        return result;
                    }
                }
            }

            return result;
        }

        private static bool IsColorMatch(int r1, int g1, int b1, Color c2)
        {
            int rDiff = r1 - c2.R;
            int gDiff = g1 - c2.G;
            int bDiff = b1 - c2.B;
            return ((rDiff * rDiff) + (gDiff * gDiff) + (bDiff * bDiff)) < (Tolerance * Tolerance);
        }
    }
}
