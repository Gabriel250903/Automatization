using System.ComponentModel;
using System.Runtime.CompilerServices;
using BackgroundType = Automatization.Types.BackgroundType;

namespace Automatization.ViewModels
{
    public class CustomTheme : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null
        )
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private string _name = "Untitled";
        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        private string _textColor = "#FF000000";
        public string TextColor
        {
            get => _textColor;
            set => SetField(ref _textColor, value);
        }

        private string _buttonBackgroundColor = "#33000000";
        public string ButtonBackgroundColor
        {
            get => _buttonBackgroundColor;
            set => SetField(ref _buttonBackgroundColor, value);
        }

        private string _buttonHoverColor = "#55000000";
        public string ButtonHoverColor
        {
            get => _buttonHoverColor;
            set => SetField(ref _buttonHoverColor, value);
        }

        private string _accentColor = "#FF0078D7";
        public string AccentColor
        {
            get => _accentColor;
            set => SetField(ref _accentColor, value);
        }

        private BackgroundType _backgroundMode = BackgroundType.Solid;
        public BackgroundType BackgroundMode
        {
            get => _backgroundMode;
            set => SetField(ref _backgroundMode, value);
        }

        private string _windowBackgroundColor = "#FFFFFFFF";
        public string WindowBackgroundColor
        {
            get => _windowBackgroundColor;
            set => SetField(ref _windowBackgroundColor, value);
        }

        private string _windowGradientEndColor = "#FFDDDDDD";
        public string WindowGradientEndColor
        {
            get => _windowGradientEndColor;
            set => SetField(ref _windowGradientEndColor, value);
        }

        private string? _backgroundImagePath = null;
        public string? BackgroundImagePath
        {
            get => _backgroundImagePath;
            set => SetField(ref _backgroundImagePath, value);
        }
    }
}
