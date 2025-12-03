using Automatization.Types;
using System.Drawing.Imaging;

namespace Automatization.Services
{
    public class HealthBarDetector
    {
        private Rectangle _lastKnownRegion = Rectangle.Empty;
        private const int SearchStrideY = 5;
        private const int SearchStrideX = 5;
        private const int ColorTolerance = 45;
        private const int MinBarWidth = 50;
        private const int MinBarHeight = 4;
        private const int MaxBarHeight = 50;
        private readonly object _lock = new();

        private readonly List<HealthColorStruct> _colorModes =
        [
            new HealthColorStruct("#F03416", "#A11609", TeamMode.Red),
            new HealthColorStruct("#4797FF", "#2148FF", TeamMode.Blue),
            new HealthColorStruct("#4BBF1D", "#255E0F", TeamMode.Green)
        ];

        public void SetCustomColors(Color bright, Color dark)
        {
            lock (_lock)
            {
                _colorModes.Clear();
                _colorModes.Add(new HealthColorStruct { Bright = bright, Dark = dark, Mode = TeamMode.Unknown });
            }
        }

        private int _maxBarWidth = 0;
        private Rectangle _maxBarBounds = Rectangle.Empty;

        public unsafe HealthBarStruct Detect(Bitmap bmp)
        {
            lock (_lock)
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                try
                {
                    HealthBarStruct result = new() { IsFound = false };

                    if (!_lastKnownRegion.IsEmpty)
                    {
                        Rectangle searchArea = _lastKnownRegion;
                        searchArea.Inflate(50, 50);
                        searchArea.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));

                        result = ScanRegion(data, searchArea, true);
                    }

                    if (!result.IsFound)
                    {
                        result = ScanRegion(data, new Rectangle(0, 0, bmp.Width, bmp.Height), false);
                    }

                    if (result.IsFound)
                    {
                        _lastKnownRegion = result.Bounds;

                        if (result.Bounds.Width > _maxBarWidth)
                        {
                            _maxBarWidth = result.Bounds.Width;
                            _maxBarBounds = result.Bounds;
                        }

                        if (result.HealthPercentage > 99.0 && _maxBarWidth > 0)
                        {
                            double widthRatio = (double)result.Bounds.Width / _maxBarWidth;

                            if (widthRatio < 0.90 && _maxBarBounds.Contains(result.Bounds.Location))
                            {
                                result.HealthPercentage = widthRatio * 100.0;
                            }
                        }
                    }
                    else
                    {
                        _lastKnownRegion = Rectangle.Empty;
                    }

                    return result;
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        private unsafe HealthBarStruct ScanRegion(BitmapData data, Rectangle area, bool precise)
        {
            int bytesPerPixel = 3;
            byte* ptr = (byte*)data.Scan0;
            int stride = data.Stride;

            int startY = area.Top;
            int endY = area.Bottom;
            int startX = area.Left;
            int endX = area.Right;

            int stepY = precise ? 1 : SearchStrideY;
            int stepX = precise ? 1 : SearchStrideX;

            for (int y = startY; y < endY; y += stepY)
            {
                byte* row = ptr + (y * stride);

                for (int x = startX; x < endX; x += stepX)
                {
                    int b = row[x * bytesPerPixel];
                    int g = row[(x * bytesPerPixel) + 1];
                    int r = row[(x * bytesPerPixel) + 2];

                    foreach (HealthColorStruct mode in _colorModes)
                    {
                        if (IsColorMatch(r, g, b, mode.Bright))
                        {
                            if (VerifyBar(data, x, y, mode, out Rectangle barBounds, out double health))
                            {
                                return new HealthBarStruct
                                {
                                    IsFound = true,
                                    Mode = mode.Mode,
                                    Bounds = barBounds,
                                    HealthPercentage = health
                                };
                            }
                        }
                    }
                }
            }

            return new HealthBarStruct { IsFound = false };
        }

        private unsafe bool VerifyBar(BitmapData data, int hitX, int hitY, HealthColorStruct mode, out Rectangle bounds, out double health)
        {
            bounds = Rectangle.Empty;
            health = 0;

            int width = data.Width;
            int height = data.Height;
            int stride = data.Stride;
            byte* ptr = (byte*)data.Scan0;

            int left = hitX;
            int right = hitX;

            while (left > 0)
            {
                byte* p = ptr + (hitY * stride) + ((left - 1) * 3);
                if (!IsColorMatch(p[2], p[1], p[0], mode.Bright) && !IsColorMatch(p[2], p[1], p[0], mode.Dark))
                {
                    break;
                }

                left--;
            }

            while (right < width - 1)
            {
                byte* p = ptr + (hitY * stride) + ((right + 1) * 3);
                if (!IsColorMatch(p[2], p[1], p[0], mode.Bright) && !IsColorMatch(p[2], p[1], p[0], mode.Dark))
                {
                    break;
                }

                right++;
            }

            int barWidth = right - left + 1;
            if (barWidth < MinBarWidth)
            {
                return false;
            }

            int midX = left + (barWidth / 2);
            int top = hitY;
            int bottom = hitY;

            while (top > 0)
            {
                byte* p = ptr + ((top - 1) * stride) + (midX * 3);
                if (!IsColorMatch(p[2], p[1], p[0], mode.Bright) && !IsColorMatch(p[2], p[1], p[0], mode.Dark))
                {
                    break;
                }

                top--;
            }

            while (bottom < height - 1)
            {
                byte* p = ptr + ((bottom + 1) * stride) + (midX * 3);
                if (!IsColorMatch(p[2], p[1], p[0], mode.Bright) && !IsColorMatch(p[2], p[1], p[0], mode.Dark))
                {
                    break;
                }

                bottom++;
            }

            int barHeight = bottom - top + 1;
            if (barHeight is < MinBarHeight or > MaxBarHeight)
            {
                return false;
            }

            if ((double)barWidth / barHeight < 2.0)
            {
                return false;
            }

            int scanY = top + (barHeight / 2);
            int brightPixels = 0;
            int darkPixels = 0;
            byte* scanRow = ptr + (scanY * stride);

            for (int x = left; x <= right; x++)
            {
                int idx = x * 3;
                int b = scanRow[idx];
                int g = scanRow[idx + 1];
                int r = scanRow[idx + 2];

                if (IsColorMatch(r, g, b, mode.Bright))
                {
                    brightPixels++;
                }
                else if (IsColorMatch(r, g, b, mode.Dark))
                {
                    darkPixels++;
                }
            }

            int totalPixels = brightPixels + darkPixels;
            if (totalPixels == 0)
            {
                return false;
            }

            double validRatio = (double)totalPixels / barWidth;
            if (validRatio < 0.8)
            {
                return false;
            }

            bounds = new Rectangle(left, top, barWidth, barHeight);
            health = (double)brightPixels / totalPixels * 100.0;
            return true;
        }

        private static bool IsColorMatch(int r1, int g1, int b1, Color c2)
        {
            int rDiff = r1 - c2.R;
            int gDiff = g1 - c2.G;
            int bDiff = b1 - c2.B;

            return ((rDiff * rDiff) + (gDiff * gDiff) + (bDiff * bDiff)) < (ColorTolerance * ColorTolerance);
        }
    }
}
