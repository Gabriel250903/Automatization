using Automatization.Services;
using Automatization.Settings;
using Automatization.ViewModels;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace Automatization.UI
{
    public partial class ReportIssueWindow : FluentWindow
    {
        public ReportIssueWindow()
        {
            InitializeComponent();
            AppSettings appSettings = AppSettings.Load();
            WebhookService webhookService = new(appSettings);

            static async void showMessageAction(string title, string message)
            {
                MessageBox uiMessageBox = new()
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = "OK"
                };
                _ = await uiMessageBox.ShowDialogAsync();
            }

            DataContext = new ReportIssueViewModel(result => { DialogResult = result; Close(); }, webhookService, AppSettings.Load, showMessageAction);
        }
    }
}
