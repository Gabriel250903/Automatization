using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using Automatization.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ClipBoard = System.Windows.Clipboard;

namespace Automatization.ViewModels
{
    public class ReportIssueViewModel : INotifyPropertyChanged
    {
        private const int MAX_DESCRIPTION_LENGTH = 1900;

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                    OnPropertyChanged(nameof(DescriptionCharCount));
                    OnPropertyChanged(nameof(IsDescriptionOverLimit));
                    OnPropertyChanged(nameof(DescriptionCounterText));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public int DescriptionCharCount => _description.Length;
        public int DescriptionMaxCharLimit => MAX_DESCRIPTION_LENGTH;
        public bool IsDescriptionOverLimit => DescriptionCharCount > MAX_DESCRIPTION_LENGTH;
        public string DescriptionCounterText => $"{DescriptionCharCount}/{DescriptionMaxCharLimit} characters";

        private bool _includeLogFile = true;
        public bool IncludeLogFile
        {
            get => _includeLogFile;
            set
            {
                if (_includeLogFile != value)
                {
                    _includeLogFile = value;
                    OnPropertyChanged(nameof(IncludeLogFile));
                }
            }
        }

        private bool _isIdea;
        public bool IsIdea
        {
            get => _isIdea;
            set
            {
                if (_isIdea != value)
                {
                    _isIdea = value;
                    OnPropertyChanged(nameof(IsIdea));
                }
            }
        }

        public ObservableCollection<string> Screenshots { get; } = [];

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddScreenshotCommand { get; }
        public ICommand PasteScreenshotCommand { get; }
        public ICommand RemoveScreenshotCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private Action<bool?> _closeAction;
        private Action<string, string> _showMessageAction;
        private readonly WebhookService _webhookService;
        private readonly Func<AppSettings> _getAppSettings;

        public ReportIssueViewModel(Action<bool?> closeAction, WebhookService webhookService, Func<AppSettings> getAppSettings, Action<string, string> showMessageAction)
        {
            _closeAction = closeAction;
            _webhookService = webhookService;
            _getAppSettings = getAppSettings;
            _showMessageAction = showMessageAction;
            SubmitCommand = new RelayCommand(async parameter => await ExecuteSubmit(parameter), CanExecuteSubmit);
            CancelCommand = new RelayCommand(ExecuteCancel);
            AddScreenshotCommand = new RelayCommand(ExecuteAddScreenshot, CanExecuteAddScreenshot);
            PasteScreenshotCommand = new RelayCommand(ExecutePasteScreenshot, CanExecutePasteScreenshot);
            RemoveScreenshotCommand = new RelayCommand(ExecuteRemoveScreenshot);

            OnPropertyChanged(nameof(DescriptionCharCount));
            OnPropertyChanged(nameof(IsDescriptionOverLimit));
            OnPropertyChanged(nameof(DescriptionCounterText));
            OnPropertyChanged(nameof(IsIdea));
        }

        private bool CanExecuteAddScreenshot(object? parameter)
        {
            return Screenshots.Count < 4;
        }

        private void ExecuteAddScreenshot(object? parameter)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    if (Screenshots.Count < 4 && !Screenshots.Contains(fileName))
                    {
                        Screenshots.Add(fileName);
                    }
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool CanExecutePasteScreenshot(object? parameter)
        {
            return Screenshots.Count < 4 && ClipBoard.ContainsImage();
        }

        private void ExecutePasteScreenshot(object? parameter)
        {
            if (ClipBoard.ContainsImage())
            {
                try
                {
                    BitmapSource image = ClipBoard.GetImage();
                    string tempFilePath = Path.Combine(Path.GetTempPath(), $"paste_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.png");

                    using (FileStream fileStream = new(tempFilePath, FileMode.Create))
                    {
                        PngBitmapEncoder encoder = new();
                        encoder.Frames.Add(BitmapFrame.Create(image));
                        encoder.Save(fileStream);
                    }

                    Screenshots.Add(tempFilePath);
                    CommandManager.InvalidateRequerySuggested();
                }
                catch (Exception ex)
                {
                    _showMessageAction("Error", $"Failed to paste image: {ex.Message}");
                }
            }
        }

        private void ExecuteRemoveScreenshot(object? parameter)
        {
            if (parameter is string filePath && Screenshots.Contains(filePath))
            {
                _ = Screenshots.Remove(filePath);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool CanExecuteSubmit(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Description) && !IsDescriptionOverLimit;
        }

        private async Task ExecuteSubmit(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                _showMessageAction("Validation Error", "Please provide a title for your report.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                _showMessageAction("Validation Error", "Please provide a description for your report.");
                return;
            }

            if (IsDescriptionOverLimit)
            {
                _showMessageAction("Validation Error", $"Description exceeds the maximum allowed length of {MAX_DESCRIPTION_LENGTH} characters.");
                return;
            }

            string? logFilePath = IncludeLogFile ? LogService.GetLatestLogFilePath() : null;

            bool success = await _webhookService.SendReportAsync(Title, Description, IsIdea, logFilePath, Screenshots);

            if (success)
            {
                LogService.LogInfo("Attempting to save report locally.");

                List<string> persistentScreenshots = [];
                try
                {
                    string screenshotsDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "TankAutomation",
                        "Screenshots"
                    );

                    if (!Directory.Exists(screenshotsDir))
                    {
                        _ = Directory.CreateDirectory(screenshotsDir);
                    }

                    foreach (string screenshotPath in Screenshots)
                    {
                        if (File.Exists(screenshotPath))
                        {
                            string fileName = Path.GetFileName(screenshotPath);
                            string uniqueFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}_{fileName}";
                            string destPath = Path.Combine(screenshotsDir, uniqueFileName);
                            File.Copy(screenshotPath, destPath);
                            persistentScreenshots.Add(destPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError("Failed to save screenshots locally.", ex);
                }

                ReportService.AddReport(new ReportItem
                {
                    Title = Title,
                    Description = Description,
                    IsIdea = IsIdea,
                    Timestamp = DateTime.Now,
                    LogFilePath = logFilePath,
                    ScreenshotPaths = persistentScreenshots,
                    Status = ReportStatus.Submitted
                });
                LogService.LogInfo("Report saved locally successfully.");
                _showMessageAction("Report Submitted", "Your report has been successfully submitted!");
            }
            else
            {
                _showMessageAction("Submission Failed", "There was an error submitting your report. Please check your internet connection or try again later.");
            }

            _closeAction?.Invoke(success);
        }

        private void ExecuteCancel(object? parameter)
        {
            _closeAction?.Invoke(false);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
