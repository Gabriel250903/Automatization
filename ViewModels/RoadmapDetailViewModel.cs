using Automatization.Services;
using Automatization.Types;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Automatization.ViewModels
{
    public class RoadmapDetailViewModel : INotifyPropertyChanged
    {
        private readonly RoadmapService _roadmapService;
        private bool _isLoading;
        public event PropertyChangedEventHandler? PropertyChanged;
        public RoadmapItem SelectedItem { get; }
        public ObservableCollection<IssueComment> Comments { get; } = [];

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

        public RoadmapDetailViewModel(RoadmapItem item)
        {
            SelectedItem = item;
            _roadmapService = new RoadmapService();
            _ = LoadCommentsAsync();
        }

        public async Task LoadCommentsAsync()
        {
            IsLoading = true;
            Comments.Clear();

            try
            {
                List<IssueComment> comments = await _roadmapService.GetIssueCommentsAsync(SelectedItem.Number);
                foreach (IssueComment comment in comments)
                {
                    comment.ParentIssueNumber = SelectedItem.Number;
                    Comments.Add(comment);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
