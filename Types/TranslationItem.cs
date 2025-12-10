using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automatization.Types
{
    public class TranslationItem : INotifyPropertyChanged
    {
        public string Key { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string OriginalValue { get; set; } = string.Empty;

        private string _translatedValue = string.Empty;
        public string TranslatedValue
        {
            get => _translatedValue;
            set
            {
                if (_translatedValue != value)
                {
                    _translatedValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
