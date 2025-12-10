using Automatization.Types;
using Automatization.ViewModels;
using System.Windows;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace Automatization.UI
{
    public partial class ReportViewerWindow : FluentWindow
    {
        public ReportViewerWindow()
        {
            InitializeComponent();

            void openReportDetailWindowAction(ReportItem report)
            {
                async void showMessageActionForDetails(string title, string message)
                {
                    MessageBox uiMessageBox = new()
                    {
                        Title = title,
                        Content = message,
                        CloseButtonText = "OK"
                    };
                    _ = await uiMessageBox.ShowDialogAsync();
                }

                void openLogContentWindowAction(string title, string content)
                {
                    LogContentWindow logContentWindow = new(content, title) { Owner = this };
                    _ = logContentWindow.ShowDialog();
                }

                void openImageViewerWindowAction(string imagePath)
                {
                    ImageViewerWindow imageViewerWindow = new(imagePath) { Owner = this };
                    _ = imageViewerWindow.ShowDialog();
                }

                ReportDetailViewModel detailViewModel = new(report, showMessageActionForDetails, openLogContentWindowAction, openImageViewerWindowAction);
                ReportDetailWindow detailWindow = new(detailViewModel) { Owner = this };
                _ = detailWindow.ShowDialog();
            }
            ;

            DataContext = new ReportViewerViewModel(openReportDetailWindowAction);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ReportNewIssueButton_Click(object sender, RoutedEventArgs e)
        {
            ReportIssueWindow reportIssueWindow = new() { Owner = this };
            bool? result = reportIssueWindow.ShowDialog();

            if (result == true)
            {
                if (DataContext is ReportViewerViewModel viewModel)
                {
                    viewModel.ReloadReports();
                }
            }
        }

        private void ReportViewerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ReportViewerViewModel viewModel)
            {
                viewModel.ReloadReports();
            }
        }
    }
}
