using Automatization.Types;
using Automatization.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace Automatization.UI
{
    public partial class RoadmapDetailWindow : FluentWindow
    {
        public RoadmapDetailWindow(RoadmapItem item)
        {
            InitializeComponent();
            DataContext = new RoadmapDetailViewModel(item);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FlowDocumentScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = false;
        }

        private void DescriptionFlowDocumentViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.Image clickedImage && clickedImage.Source is BitmapImage bitmapImage)
            {
                string? imagePath = bitmapImage.UriSource?.LocalPath;

                if (!string.IsNullOrEmpty(imagePath))
                {
                    ImageViewerWindow imageViewer = new(imagePath)
                    {
                        Owner = this
                    };

                    _ = imageViewer.ShowDialog();
                    e.Handled = true;
                }
            }
        }
    }
}
