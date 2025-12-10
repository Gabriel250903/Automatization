using Automatization.Services;
using Automatization.Types;
using Automatization.Utils;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace Automatization.ViewModels
{
    public class ReportDetailViewModel : INotifyPropertyChanged
    {
        private ReportItem _report;
        public ReportItem Report
        {
            get => _report;
            set
            {
                if (_report != value)
                {
                    _report = value;
                    OnPropertyChanged(nameof(Report));
                    OnPropertyChanged(nameof(ReportType));
                    OnPropertyChanged(nameof(HasLogFile));
                    OnPropertyChanged(nameof(ViewLogToolTip));
                    OnPropertyChanged(nameof(Screenshots));
                    OnPropertyChanged(nameof(HasScreenshots));
                }
            }
        }

        public string ReportType => Report.IsIdea ? "Idea Report" : "Issue Report";

        public bool HasLogFile => !string.IsNullOrEmpty(Report.LogFilePath) && File.Exists(Report.LogFilePath);

        public string ViewLogToolTip => HasLogFile ? "View log file" : "No log file attached";

        public List<string> Screenshots => Report.ScreenshotPaths;

        public bool HasScreenshots => Report.ScreenshotPaths != null && Report.ScreenshotPaths.Count > 0;

        public ICommand ViewLogCommand { get; }
        public ICommand OpenImageCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private Action<string, string> _showMessageAction;
        private Action<string, string> _openLogContentWindowAction;
        private Action<string> _openImageViewerWindowAction;

        public ReportDetailViewModel(ReportItem report, Action<string, string> showMessageAction, Action<string, string> openLogContentWindowAction, Action<string> openImageViewerWindowAction)
        {
            _report = report;
            _showMessageAction = showMessageAction;
            _openLogContentWindowAction = openLogContentWindowAction;
            _openImageViewerWindowAction = openImageViewerWindowAction;

            ViewLogCommand = new RelayCommand(ExecuteViewLog, CanExecuteViewLog);
            OpenImageCommand = new RelayCommand(ExecuteOpenImage);

            OnPropertyChanged(nameof(ViewLogToolTip));
            OnPropertyChanged(nameof(Screenshots));
            OnPropertyChanged(nameof(HasScreenshots));
        }

        private bool CanExecuteViewLog(object? parameter)
        {
            return HasLogFile;
        }

        private void ExecuteOpenImage(object? parameter)
        {
            if (parameter is string imagePath && File.Exists(imagePath))
            {
                _openImageViewerWindowAction?.Invoke(imagePath);
            }
        }

        private void ExecuteViewLog(object? parameter)
        {
            if (HasLogFile && !string.IsNullOrEmpty(Report.LogFilePath))
            {
                string? tempLogFilePath = null;
                try
                {
                    tempLogFilePath = Path.GetTempFileName();
                    File.Copy(Report.LogFilePath, tempLogFilePath, true);

                    string logContent = File.ReadAllText(tempLogFilePath);
                    _openLogContentWindowAction($"Log for {Report.Title}", logContent);
                }
                catch (Exception ex)
                {
                    _showMessageAction("Error", $"Could not read log file: {ex.Message}");
                    LogService.LogError($"Error reading log file {Report.LogFilePath}: {ex.Message}");
                }
                finally
                {
                    if (!string.IsNullOrEmpty(tempLogFilePath) && File.Exists(tempLogFilePath))
                    {
                        try
                        {
                            File.Delete(tempLogFilePath);
                        }
                        catch (Exception ex)
                        {
                            LogService.LogError($"Failed to delete temporary log file: {tempLogFilePath}. Error: {ex.Message}");
                        }
                    }
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
