using Automatization.Hotkeys;
using Automatization.Settings;
using Automatization.Types;
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

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = App.Settings ?? AppSettings.Load();
            LoadSettings();

            GlobalHotKeyBox.LostFocus += HotKeyBox_LostFocus;
            RedTeamHotKeyBox.LostFocus += HotKeyBox_LostFocus;
            BlueTeamHotKeyBox.LostFocus += HotKeyBox_LostFocus;
        }

        private void LoadSettings()
        {
            ClickSpeedTextBox.Text = _settings.ClickSpeed.ToString(CultureInfo.InvariantCulture);

            GameProcessNameTextBox.Text = _settings.GameProcessName;

            GlobalHotKeyBox.HotKey = _settings.GlobalHotKey;
            RedTeamHotKeyBox.HotKey = _settings.RedTeamHotKey;
            BlueTeamHotKeyBox.HotKey = _settings.BlueTeamHotKey;
            GamePathTextBox.Text = _settings.GameExecutablePath;

            // Load Clicker Coordinates
            RedTeamXTextBox.Text = _settings.RedTeamCoordinates.X.ToString(CultureInfo.InvariantCulture);
            RedTeamYTextBox.Text = _settings.RedTeamCoordinates.Y.ToString(CultureInfo.InvariantCulture);
            BlueTeamXTextBox.Text = _settings.BlueTeamCoordinates.X.ToString(CultureInfo.InvariantCulture);
            BlueTeamYTextBox.Text = _settings.BlueTeamCoordinates.Y.ToString(CultureInfo.InvariantCulture);

            // Load Powerup Keys
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

            UpdateHotKeyConflictWarnings(true);
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

            if (GlobalHotKeyConflictWarning.IsVisible || RedTeamHotKeyConflictWarning.IsVisible || BlueTeamHotKeyConflictWarning.IsVisible)
            {
                return;
            }


            if (double.TryParse(ClickSpeedTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double clickSpeed) && clickSpeed >= 1)
            {
                _settings.ClickSpeed = clickSpeed;
            }
            else
            {
                return;
            }
            _settings.GameProcessName = !string.IsNullOrWhiteSpace(GameProcessNameTextBox.Text) ? GameProcessNameTextBox.Text.Trim() : "ProTanki";

            _settings.GameExecutablePath = GamePathTextBox.Text;

            _settings.GlobalHotKey = GlobalHotKeyBox.HotKey;
            _settings.RedTeamHotKey = RedTeamHotKeyBox.HotKey;
            _settings.BlueTeamHotKey = BlueTeamHotKeyBox.HotKey;

            // Save Clicker Coordinates
            if (double.TryParse(RedTeamXTextBox.Text, out double rX) && double.TryParse(RedTeamYTextBox.Text, out double rY))
            {
                _settings.RedTeamCoordinates = new Point(rX, rY);
            }
            else
            {
                return;
            }

            if (double.TryParse(BlueTeamXTextBox.Text, out double bX) && double.TryParse(BlueTeamYTextBox.Text, out double bY))
            {
                _settings.BlueTeamCoordinates = new Point(bX, bY);
            }
            else
            {
                return;
            }


            // Save Powerup Keys
            _settings.PowerupKeys[PowerupType.RepairKit] = RepairKitKeyBox.HotKey.Key; // Ignores modifiers
            _settings.PowerupKeys[PowerupType.DoubleArmor] = DoubleArmorKeyBox.HotKey.Key; // Ignores modifiers
            _settings.PowerupKeys[PowerupType.DoubleDamage] = DoubleDamageKeyBox.HotKey.Key; // Ignores modifiers
            _settings.PowerupKeys[PowerupType.SpeedBoost] = SpeedBoostKeyBox.HotKey.Key; // Ignores modifiers
            _settings.PowerupKeys[PowerupType.Mine] = MineKeyBox.HotKey.Key; // Ignores modifiers

            _settings.Theme = DarkRadio.IsChecked == true ? ThemeType.Dark : ThemeType.Light;

            _settings.Save();

            Close();
        }

        private void HotKeyBox_LostFocus(object? sender, RoutedEventArgs e)
        {
            UpdateHotKeyConflictWarnings(false);
        }

        private void UpdateHotKeyConflictWarnings(bool isInitializing)
        {
            HotKey global = GlobalHotKeyBox.HotKey;
            HotKey red = RedTeamHotKeyBox.HotKey;
            HotKey blue = BlueTeamHotKeyBox.HotKey;

            GlobalHotKeyConflictWarning.Visibility = Visibility.Collapsed;
            RedTeamHotKeyConflictWarning.Visibility = Visibility.Collapsed;
            BlueTeamHotKeyConflictWarning.Visibility = Visibility.Collapsed;

            bool hasConflict = false;

            if (!global.IsEmpty)
            {
                if (global == red)
                {
                    hasConflict = true;
                    GlobalHotKeyConflictWarning.Visibility = Visibility.Visible;
                    RedTeamHotKeyConflictWarning.Visibility = Visibility.Visible;
                }
                if (global == blue)
                {
                    hasConflict = true;
                    GlobalHotKeyConflictWarning.Visibility = Visibility.Visible;
                    BlueTeamHotKeyConflictWarning.Visibility = Visibility.Visible;
                }
            }

            if (!red.IsEmpty && red == blue)
            {
                hasConflict = true;
                RedTeamHotKeyConflictWarning.Visibility = Visibility.Visible;
                BlueTeamHotKeyConflictWarning.Visibility = Visibility.Visible;
            }

            if (hasConflict && !isInitializing)
            {
                GlobalHotKeyBox.HotKey = _settings.GlobalHotKey;
                RedTeamHotKeyBox.HotKey = _settings.RedTeamHotKey;
                BlueTeamHotKeyBox.HotKey = _settings.BlueTeamHotKey;

                UpdateHotKeyConflictWarnings(true);

            }
        }

        private void LogsButton_Click(object sender, RoutedEventArgs e)
        {
            var logViewer = new LogViewerWindow { Owner = this };
            logViewer.ShowDialog();
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

    }
}
