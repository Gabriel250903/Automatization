using Automatization.Hotkeys;
using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using Automatization.UI;
using Automatization.UI.Coordinate;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;

namespace Automatization
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        private bool _isGameProcessNameUnlocked = false;

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = App.Settings ?? AppSettings.Load();
            LoadSettings();
        }

        private void LoadSettings()
        {
            ClickSpeedTextBox.Text = _settings.ClickSpeed.ToString(CultureInfo.InvariantCulture);

            GameProcessNameTextBox.Text = _settings.GameProcessName;
            GameProcessNameTextBox.IsReadOnly = !_isGameProcessNameUnlocked;
            UnlockGameProcessNameButton.Visibility = _isGameProcessNameUnlocked ? Visibility.Collapsed : Visibility.Visible;

            GlobalHotKeyBox.HotKey = _settings.GlobalHotKey;
            RedTeamHotKeyBox.HotKey = _settings.RedTeamHotKey;
            BlueTeamHotKeyBox.HotKey = _settings.BlueTeamHotKey;
            GamePathTextBox.Text = _settings.GameExecutablePath;

            RedTeamXTextBox.Text = _settings.RedTeamCoordinates.X.ToString(CultureInfo.InvariantCulture);
            RedTeamYTextBox.Text = _settings.RedTeamCoordinates.Y.ToString(CultureInfo.InvariantCulture);
            BlueTeamXTextBox.Text = _settings.BlueTeamCoordinates.X.ToString(CultureInfo.InvariantCulture);
            BlueTeamYTextBox.Text = _settings.BlueTeamCoordinates.Y.ToString(CultureInfo.InvariantCulture);

            RepairKitKeyBox.HotKey = new HotKey(_settings.PowerupKeys.GetValueOrDefault(PowerupType.RepairKit, Key.D1), ModifierKeys.None);
            DoubleArmorKeyBox.HotKey = new HotKey(_settings.PowerupKeys.GetValueOrDefault(PowerupType.DoubleArmor, Key.D2), ModifierKeys.None);
            DoubleDamageKeyBox.HotKey = new HotKey(_settings.PowerupKeys.GetValueOrDefault(PowerupType.DoubleDamage, Key.D3), ModifierKeys.None);
            SpeedBoostKeyBox.HotKey = new HotKey(_settings.PowerupKeys.GetValueOrDefault(PowerupType.SpeedBoost, Key.D4), ModifierKeys.None);
            MineKeyBox.HotKey = new HotKey(_settings.PowerupKeys.GetValueOrDefault(PowerupType.Mine, Key.D5), ModifierKeys.None);

            if (_settings.Theme == ThemeType.Light)
            {
                LightRadio.IsChecked = true;
            }
            else
            {
                DarkRadio.IsChecked = true;
            }

            RegisterSettingsHotkeys();
        }

        private void RegisterSettingsHotkeys()
        {
            GlobalHotKeyManager.UnregisterAll();

            _ = GlobalHotKeyManager.Register(_settings.GlobalHotKey);
            _ = GlobalHotKeyManager.Register(_settings.RedTeamHotKey);
            _ = GlobalHotKeyManager.Register(_settings.BlueTeamHotKey);

            foreach (KeyValuePair<PowerupType, Key> entry in _settings.PowerupKeys)
            {
                HotKey powerupHotKey = new(entry.Value, ModifierKeys.None);
                _ = GlobalHotKeyManager.Register(powerupHotKey);
            }
        }
        private void ThemeRadio_Checked(object? sender, RoutedEventArgs e)
        {
            if (DarkRadio.IsChecked == true)
            {
                App.ApplyTheme(ThemeType.Dark);
            }
            else if (LightRadio.IsChecked == true)
            {
                App.ApplyTheme(ThemeType.Light);
            }
        }

        private void BrowseGamePathButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Game Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                GamePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(this, SaveButton);

            HotKey originalGlobalHotKey = _settings.GlobalHotKey;
            HotKey originalRedTeamHotKey = _settings.RedTeamHotKey;
            HotKey originalBlueTeamHotKey = _settings.BlueTeamHotKey;
            Dictionary<PowerupType, Key> originalPowerupKeys = new(_settings.PowerupKeys);

            if (double.TryParse(ClickSpeedTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double clickSpeed) && clickSpeed >= 1)
            {
                _settings.ClickSpeed = clickSpeed;
            }
            else
            {
                LogService.LogError("Click speed must be a number greater than or equal to 1.");
                return;
            }
            _settings.GameProcessName = !string.IsNullOrWhiteSpace(GameProcessNameTextBox.Text) ? GameProcessNameTextBox.Text.Trim() : "ProTanki";

            _settings.GameExecutablePath = GamePathTextBox.Text;

            _settings.GlobalHotKey = GlobalHotKeyBox.HotKey;
            _settings.RedTeamHotKey = RedTeamHotKeyBox.HotKey;
            _settings.BlueTeamHotKey = BlueTeamHotKeyBox.HotKey;

            if (double.TryParse(RedTeamXTextBox.Text, out double rX) && double.TryParse(RedTeamYTextBox.Text, out double rY))
            {
                _settings.RedTeamCoordinates = new Point(rX, rY);
            }
            else
            {
                LogService.LogError("Invalid Red Team coordinates.");
                return;
            }

            if (double.TryParse(BlueTeamXTextBox.Text, out double bX) && double.TryParse(BlueTeamYTextBox.Text, out double bY))
            {
                _settings.BlueTeamCoordinates = new Point(bX, bY);
            }
            else
            {
                LogService.LogError("Invalid Blue Team coordinates.");
                return;
            }


            _settings.PowerupKeys[PowerupType.RepairKit] = RepairKitKeyBox.HotKey.Key;
            _settings.PowerupKeys[PowerupType.DoubleArmor] = DoubleArmorKeyBox.HotKey.Key;
            _settings.PowerupKeys[PowerupType.DoubleDamage] = DoubleDamageKeyBox.HotKey.Key;
            _settings.PowerupKeys[PowerupType.SpeedBoost] = SpeedBoostKeyBox.HotKey.Key;
            _settings.PowerupKeys[PowerupType.Mine] = MineKeyBox.HotKey.Key;

            _settings.Theme = DarkRadio.IsChecked == true ? ThemeType.Dark : ThemeType.Light;

            GlobalHotKeyManager.UnregisterAll();

            bool registrationFailed = false;

            if (!GlobalHotKeyManager.Register(_settings.GlobalHotKey))
            {
                LogService.LogError($"Failed to register Global Hotkey: {_settings.GlobalHotKey}");
                registrationFailed = true;
            }
            if (!GlobalHotKeyManager.Register(_settings.RedTeamHotKey))
            {
                LogService.LogError($"Failed to register Red Team Hotkey: {_settings.RedTeamHotKey}");
                registrationFailed = true;
            }
            if (!GlobalHotKeyManager.Register(_settings.BlueTeamHotKey))
            {
                LogService.LogError($"Failed to register Blue Team Hotkey: {_settings.BlueTeamHotKey}");
                registrationFailed = true;
            }

            foreach (KeyValuePair<PowerupType, Key> entry in _settings.PowerupKeys)
            {
                HotKey powerupHotKey = new(entry.Value, ModifierKeys.None);
                if (!GlobalHotKeyManager.Register(powerupHotKey))
                {
                    LogService.LogError($"Failed to register Powerup Hotkey {entry.Key}: {powerupHotKey}");
                    registrationFailed = true;
                }
            }


            if (registrationFailed)
            {
                LogService.LogError("One or more hotkeys could not be registered. They might be in use by another application. Reverting to previous hotkeys.");

                _settings.GlobalHotKey = originalGlobalHotKey;
                _settings.RedTeamHotKey = originalRedTeamHotKey;
                _settings.BlueTeamHotKey = originalBlueTeamHotKey;
                _settings.PowerupKeys = originalPowerupKeys;

                GlobalHotKeyManager.UnregisterAll();
                _ = GlobalHotKeyManager.Register(_settings.GlobalHotKey);
                _ = GlobalHotKeyManager.Register(_settings.RedTeamHotKey);
                _ = GlobalHotKeyManager.Register(_settings.BlueTeamHotKey);
                foreach (KeyValuePair<PowerupType, Key> entry in _settings.PowerupKeys)
                {
                    _ = GlobalHotKeyManager.Register(new HotKey(entry.Value, ModifierKeys.None));
                }

                LoadSettings();
                return;
            }


            _settings.Save();

            Close();
        }
        private void LogsButton_Click(object sender, RoutedEventArgs e)
        {
            LogViewerWindow logViewer = new() { Owner = this };
            _ = logViewer.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(this, CloseButton);

            _settings = AppSettings.Load();
            LoadSettings();
            App.ApplyTheme(_settings.Theme);
            Close();
        }

        private void PickRedTeamButton_Click(object sender, RoutedEventArgs e)
        {
            CoordinatePickerWindow picker = new();
            if (picker.ShowDialog() == true)
            {
                RedTeamXTextBox.Text = picker.SelectedPoint.X.ToString(CultureInfo.InvariantCulture);
                RedTeamYTextBox.Text = picker.SelectedPoint.Y.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void PickBlueTeamButton_Click(object sender, RoutedEventArgs e)
        {
            CoordinatePickerWindow picker = new();
            if (picker.ShowDialog() == true)
            {
                BlueTeamXTextBox.Text = picker.SelectedPoint.X.ToString(CultureInfo.InvariantCulture);
                BlueTeamYTextBox.Text = picker.SelectedPoint.Y.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void UnlockGameProcessNameButton_Click(object sender, RoutedEventArgs e)
        {
            InputDialog inputDialog = new()
            {
                Owner = this
            };

            if (inputDialog.ShowDialog() == true)
            {
                _isGameProcessNameUnlocked = true;
                GameProcessNameTextBox.IsReadOnly = false;
                _ = GameProcessNameTextBox.Focus();
                UnlockGameProcessNameButton.Visibility = Visibility.Collapsed;
                LogService.LogInfo("Game Process Name unlocked for editing.");
            }
            else
            {
                LogService.LogInfo("Game Process Name unlock cancelled or incorrect password entered.");
            }
        }
    }
}