using Automatization.Services;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace Automatization.UI
{
    public partial class ImageViewerWindow : FluentWindow
    {
        private const double MAX_ZOOM = 4.0;
        private Point _origin;
        private Point _start;

        public string ImagePath
        {
            get => (string)GetValue(ImagePathProperty);
            set => SetValue(ImagePathProperty, value);
        }

        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register("ImagePath", typeof(string), typeof(ImageViewerWindow), new PropertyMetadata(null, OnImagePathChanged));

        private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageViewerWindow window && e.NewValue is string path)
            {
                window.LoadImageAndFit(path);
            }
        }

        public ImageViewerWindow(string imagePath)
        {
            InitializeComponent();
            DataContext = this;
            ImagePath = imagePath;
            Loaded += ImageViewerWindow_Loaded;
            SizeChanged += ImageViewerWindow_SizeChanged;
            PreviewKeyDown += Window_PreviewKeyDown;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.D0 || e.Key == Key.NumPad0))
            {
                FitImageToWindow();
                e.Handled = true;
            }
        }

        private void ImageViewerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FitImageToWindow();
        }

        private void ImageViewerWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FitImageToWindow();
        }

        private void LoadImageAndFit(string path)
        {
            try
            {
                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                DisplayedImage.Source = bitmap;
                FitImageToWindow();
            }
            catch (Exception ex)
            {
                MessageBox uiMessageBox = new()
                {
                    Title = "Image Load Error",
                    Content = $"Failed to load image: {ex.Message}",
                    CloseButtonText = "OK",
                    Owner = this
                };
                _ = uiMessageBox.ShowDialogAsync();

                LogService.LogError($"Failed to load image {path}: {ex.Message}");
            }
        }

        private void FitImageToWindow()
        {
            if (DisplayedImage.Source is BitmapSource bitmapSource && ImageScrollViewer != null)
            {
                double imageWidth = bitmapSource.PixelWidth;
                double imageHeight = bitmapSource.PixelHeight;

                if (imageWidth == 0 || imageHeight == 0)
                {
                    return;
                }

                double viewerWidth = ImageScrollViewer.ActualWidth;
                double viewerHeight = ImageScrollViewer.ActualHeight;

                if (viewerWidth == 0 || viewerHeight == 0)
                {
                    return;
                }

                double scaleX = viewerWidth / imageWidth;
                double scaleY = viewerHeight / imageHeight;

                double scale = Math.Min(scaleX, scaleY);

                ImageScaleTransform.ScaleX = scale;
                ImageScaleTransform.ScaleY = scale;

                ImageScrollViewer.ScrollToHorizontalOffset(0);
                ImageScrollViewer.ScrollToVerticalOffset(0);
            }
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoom = e.Delta > 0 ? 0.2 : -0.2;
            double currentScale = ImageScaleTransform.ScaleX;
            double newScale = currentScale + zoom;

            newScale = Math.Clamp(newScale, 0.1, MAX_ZOOM);

            ImageScaleTransform.ScaleX = newScale;
            ImageScaleTransform.ScaleY = newScale;

            e.Handled = true;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DisplayedImage.IsMouseCaptured)
            {
                return;
            }

            _ = DisplayedImage.CaptureMouse();

            _start = e.GetPosition(ImageScrollViewer);
            _origin = new Point(ImageScrollViewer.HorizontalOffset, ImageScrollViewer.VerticalOffset);
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DisplayedImage.ReleaseMouseCapture();
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!DisplayedImage.IsMouseCaptured)
            {
                return;
            }

            Point current = e.GetPosition(ImageScrollViewer);
            Vector delta = _start - current;

            ImageScrollViewer.ScrollToHorizontalOffset(_origin.X + delta.X);
            ImageScrollViewer.ScrollToVerticalOffset(_origin.Y + delta.Y);
        }
    }
}
