using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Automatization.Macros.Execution;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingColor = System.Drawing.Color;
using DrawingGraphics = System.Drawing.Graphics;
using DrawingSize = System.Drawing.Size;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MediaColor = System.Windows.Media.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace Automatization.UI
{
    public partial class MacroCoordinatePickerWindow : Window
    {
        private DrawingBitmap? _screenSnapshot;
        private int _screenLeft;
        private int _screenTop;

        public Point SelectedPoint { get; private set; }
        public string SelectedColorHex { get; private set; } = "#FFFFFF";

        public MacroCoordinatePickerWindow()
        {
            CaptureScreenBeforeDisplay();
            InitializeComponent();

            Loaded += (s, e) =>
            {
                _ = Activate();
                _ = Focus();
            };

            Closed += (s, e) =>
            {
                _screenSnapshot?.Dispose();
                _screenSnapshot = null;
            };
        }

        private void CaptureScreenBeforeDisplay()
        {
            try
            {
                _screenLeft = SystemInformation.VirtualScreen.Left;
                _screenTop = SystemInformation.VirtualScreen.Top;
                int width = SystemInformation.VirtualScreen.Width;
                int height = SystemInformation.VirtualScreen.Height;

                _screenSnapshot = new DrawingBitmap(
                    width,
                    height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );
                using DrawingGraphics g = DrawingGraphics.FromImage(_screenSnapshot);
                g.CopyFromScreen(_screenLeft, _screenTop, 0, 0, new DrawingSize(width, height));
            }
            catch
            {
                _screenSnapshot = null;
            }
        }

        private DrawingColor GetCapturedColorAt(int screenX, int screenY)
        {
            if (_screenSnapshot != null)
            {
                int relX = screenX - _screenLeft;
                int relY = screenY - _screenTop;
                if (
                    relX >= 0
                    && relX < _screenSnapshot.Width
                    && relY >= 0
                    && relY < _screenSnapshot.Height
                )
                {
                    return _screenSnapshot.GetPixel(relX, relY);
                }
            }

            uint fallback = PixelReader.GetScreenPixelColor(screenX, screenY);
            return DrawingColor.FromArgb(
                (int)((fallback >> 16) & 0xFF),
                (int)((fallback >> 8) & 0xFF),
                (int)(fallback & 0xFF)
            );
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(this);
            Point screenPt = PointToScreen(mousePos);

            DrawingColor col = GetCapturedColorAt((int)screenPt.X, (int)screenPt.Y);
            string hex = $"#{col.R:X2}{col.G:X2}{col.B:X2}";

            TxtHudCoords.Text = $"X: {(int)screenPt.X}, Y: {(int)screenPt.Y}";
            TxtHudColor.Text = hex;
            ColorSwatch.Background = new SolidColorBrush(MediaColor.FromRgb(col.R, col.G, col.B));

            double x = mousePos.X + 20;
            double y = mousePos.Y + 20;
            if (x + 140 > ActualWidth)
            {
                x = mousePos.X - 140;
            }

            if (y + 60 > ActualHeight)
            {
                y = mousePos.Y - 60;
            }

            Canvas.SetLeft(HudBorder, Math.Max(10, x));
            Canvas.SetTop(HudBorder, Math.Max(10, y));
            HudBorder.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point screenPt = PointToScreen(e.GetPosition(this));
            SelectedPoint = screenPt;

            DrawingColor col = GetCapturedColorAt((int)screenPt.X, (int)screenPt.Y);
            SelectedColorHex = $"#{col.R:X2}{col.G:X2}{col.B:X2}";

            DialogResult = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
            }
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DialogResult = false;
        }
    }
}
