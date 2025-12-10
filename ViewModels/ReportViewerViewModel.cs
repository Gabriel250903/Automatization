using Automatization.Services;
using Automatization.Types;
using Automatization.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Automatization.ViewModels
{
    public class ReportViewerViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ReportItem> Reports { get; set; }

        public ICommand ViewReportDetailsCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private Action<ReportItem> _openReportDetailWindowAction;

        public ReportViewerViewModel(Action<ReportItem> openReportDetailWindowAction)
        {
            _openReportDetailWindowAction = openReportDetailWindowAction;
            Reports = new ObservableCollection<ReportItem>(ReportService.LoadReports());
            ViewReportDetailsCommand = new RelayCommand(ExecuteViewReportDetails, CanExecuteViewReportDetails);
        }

        public void ReloadReports()
        {
            Reports.Clear();

            foreach (ReportItem? report in ReportService.LoadReports().OrderByDescending(r => r.Timestamp))
            {
                Reports.Add(report);
            }
        }

        private bool CanExecuteViewReportDetails(object? parameter)
        {
            return parameter is ReportItem;
        }

        private void ExecuteViewReportDetails(object? parameter)
        {
            if (parameter is ReportItem selectedReport)
            {
                _openReportDetailWindowAction?.Invoke(selectedReport);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
