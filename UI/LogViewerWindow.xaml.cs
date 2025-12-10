using System.Diagnostics;
using System.IO;
using System.Windows;
using Wpf.Ui.Controls;

namespace Automatization.Settings
{
    public partial class LogViewerWindow : FluentWindow
    {
        public LogViewerWindow()
        {
            InitializeComponent();
            LoadLatestLog();
        }

        private void LoadLatestLog()
        {
            try
            {
                string logDirectory = GetLogDirectory();

                if (!Directory.Exists(logDirectory))
                {
                    LogTextBox.Text = "Log directory does not exist yet. Creating it right now...";
                    return;
                }

                DirectoryInfo directory = new(logDirectory);
                FileInfo? logFile = directory.GetFiles("*.txt")
                                       .OrderByDescending(f => f.LastWriteTime)
                                       .FirstOrDefault();

                if (logFile != null)
                {
                    string tempFilePath = Path.GetTempFileName();
                    File.Copy(logFile.FullName, tempFilePath, true);

                    LogTextBox.Text = File.ReadAllText(tempFilePath);

                    File.Delete(tempFilePath);
                    LogScrollViewer.ScrollToBottom();
                }
                else
                {
                    LogTextBox.Text = "No log files found.";
                }
            }
            catch (Exception ex)
            {
                LogTextBox.Text = $"Error loading log file: {ex.Message}";
            }
        }

        private static string GetLogDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TankAutomation",
                "Logs"
            );
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLatestLog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = GetLogDirectory();

                _ = Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                Wpf.Ui.Controls.MessageBox uiMessageBox = new()
                {
                    Title = "Error",
                    Content = $"Unable to open folder: {ex.Message}",
                    CloseButtonText = "OK",
                    Owner = this
                };

                _ = await uiMessageBox.ShowDialogAsync();
            }
        }
    }
}