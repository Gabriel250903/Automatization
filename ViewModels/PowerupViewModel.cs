using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using Automatization.Utils;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace Automatization.ViewModels
{
    public class PowerupViewModel : INotifyPropertyChanged
    {
        private AppSettings _settings;
        private Action<PowerupType> _usePowerupAction;
        private Action<PowerupType, double> _saveDelayAction;
        private DispatcherTimer _timer;

        private PowerupType _powerupType;
        public PowerupType PowerupType
        {
            get => _powerupType;
            set
            {
                if (_powerupType != value)
                {
                    _powerupType = value;
                    OnPropertyChanged(nameof(PowerupType));
                    UpdateLocalization();
                }
            }
        }

        private double _delay;
        public double Delay
        {
            get => _delay;
            set
            {
                if (_delay != value)
                {
                    _delay = value;
                    OnPropertyChanged(nameof(Delay));
                    DelayText = $"{_delay:0}ms";
                    _timer.Interval = TimeSpan.FromMilliseconds(_delay);
                    _saveDelayAction?.Invoke(PowerupType, _delay);
                }
            }
        }

        private string _delayText;
        public string DelayText
        {
            get => _delayText;
            set
            {
                if (_delayText != value)
                {
                    _delayText = value;
                    OnPropertyChanged(nameof(DelayText));
                }
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));

                    if (_isActive)
                    {
                        _timer.Start();
                    }
                    else
                    {
                        _timer.Stop();
                    }

                    UpdateLocalization();
                }
            }
        }

        private string _buttonContent = string.Empty;
        public string ButtonContent
        {
            get => _buttonContent;
            set
            {
                if (_buttonContent != value)
                {
                    _buttonContent = value;
                    OnPropertyChanged(nameof(ButtonContent));
                }
            }
        }

        public ICommand TogglePowerupCommand { get; }

        public PowerupViewModel(
            PowerupType powerupType,
            double initialDelay,
            AppSettings settings,
            Action<PowerupType> usePowerupAction,
            Action<PowerupType, double> saveDelayAction)
        {
            _powerupType = powerupType;
            _delay = initialDelay;
            _delayText = $"{initialDelay:0}ms";
            _settings = settings;
            _usePowerupAction = usePowerupAction;
            _saveDelayAction = saveDelayAction;

            TogglePowerupCommand = new RelayCommand(TogglePowerup);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_delay)
            };
            _timer.Tick += (s, e) => _usePowerupAction?.Invoke(PowerupType);

            LanguageService.LanguageChanged += (s, e) => UpdateLocalization();
            UpdateLocalization();
        }

        private void TogglePowerup(object? parameter)
        {
            IsActive = !IsActive;
        }

        private void UpdateLocalization()
        {
            string actionKey = IsActive ? "General_Stop" : "General_Start";
            string actionText = (string)Application.Current.Resources[actionKey] ?? (IsActive ? "Stop" : "Start");

            string powerupKey = PowerupType switch
            {
                PowerupType.RepairKit => "Settings_Powerup_Repair",
                PowerupType.DoubleArmor => "Settings_Powerup_Armor",
                PowerupType.DoubleDamage => "Settings_Powerup_Damage",
                PowerupType.SpeedBoost => "Settings_Powerup_Speed",
                PowerupType.Mine => "Settings_Powerup_Mine",
                _ => ""
            };

            string powerupText = (string)Application.Current.Resources[powerupKey] ?? PowerupType.ToString();

            ButtonContent = $"{actionText} {powerupText}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
