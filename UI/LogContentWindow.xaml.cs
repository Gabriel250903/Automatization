using System.Windows;
using Wpf.Ui.Controls;

namespace Automatization.UI
{
    public partial class LogContentWindow : FluentWindow
    {
        public string LogContent { get; set; }
        public string WindowTitle { get; set; }

        public LogContentWindow(string logContent, string windowTitle)
        {
            InitializeComponent();
            LogContent = logContent;
            WindowTitle = windowTitle;
            DataContext = this;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
