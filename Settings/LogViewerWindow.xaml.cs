using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Automatization.Settings
{
    public partial class LogViewerWindow : Window
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
                var directory = new DirectoryInfo(logDirectory);
                var logFile = directory.GetFiles("*.txt")
                                       .OrderByDescending(f => f.LastWriteTime)
                                       .FirstOrDefault();

                if (logFile != null)
                {
                    string tempFilePath = Path.GetTempFileName();
                    File.Copy(logFile.FullName, tempFilePath, true);
                    LogTextBox.Text = File.ReadAllText(tempFilePath);
                    File.Delete(tempFilePath);
                    LogTextBox.ScrollToEnd();
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

        private string GetLogDirectory()
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
    }
}
