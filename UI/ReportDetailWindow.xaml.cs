using Automatization.ViewModels;
using System.Windows;
using Wpf.Ui.Controls;

namespace Automatization.UI
{
    public partial class ReportDetailWindow : FluentWindow
    {
        public ReportDetailWindow()
        {
            InitializeComponent();
        }

        public ReportDetailWindow(ReportDetailViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
