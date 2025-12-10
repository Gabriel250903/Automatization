using Automatization.Services;
using Automatization.Types;
using Automatization.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace Automatization.ViewModels
{
    public class RoadmapViewModel : INotifyPropertyChanged
    {
        private readonly RoadmapService _roadmapService;
        private bool _isLoading;
        private bool _isEmpty;
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<RoadmapItem> PlannedItems { get; } = [];
        public ObservableCollection<RoadmapItem> CompletedItems { get; } = [];

        public ICommand ViewDetailsCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                if (_isEmpty != value)
                {
                    _isEmpty = value;
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public RoadmapViewModel()
        {
            _roadmapService = new RoadmapService();
            ViewDetailsCommand = new Utils.RelayCommand(ExecuteViewDetails);
            _ = LoadRoadmapAsync();
        }

        private void ExecuteViewDetails(object? parameter)
        {
            if (parameter is RoadmapItem item)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RoadmapDetailWindow detailWindow = new(item);

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.IsActive)
                        {
                            detailWindow.Owner = window;
                            break;
                        }
                    }

                    _ = detailWindow.ShowDialog();
                });
            }
        }

        public async Task LoadRoadmapAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            PlannedItems.Clear();
            CompletedItems.Clear();

            try
            {
                List<RoadmapItem> allItems = await _roadmapService.GetRoadmapAsync();

                foreach (RoadmapItem item in allItems)
                {
                    if (item.State == "open")
                    {
                        PlannedItems.Add(item);
                    }
                    else if (item.State == "closed")
                    {
                        CompletedItems.Add(item);
                    }
                }
            }
            finally
            {
                IsLoading = false;
                IsEmpty = PlannedItems.Count == 0 && CompletedItems.Count == 0;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
