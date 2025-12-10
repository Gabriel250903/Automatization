using Automatization.Hotkeys;
using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using Automatization.UI;
using Automatization.UI.Coordinate;
using Automatization.Utils;
using Automatization.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using Navigation = System.Windows.Navigation;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;

namespace Automatization
{
    public partial class SettingsWindow : FluentWindow
    {
        private AppSettings _settings;
        private UpdaterService _updaterService;
        private bool _isGameProcessNameUnlocked = false;
        private bool _isInitialized = false;
        public SettingsWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            _updaterService = new UpdaterService();

            LoadSettings();

            _isInitialized = true;
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            VersionTextBlock.Text = string.Format(
                (string)Application.Current.Resources["Settings_CurrentVersion"],
                UpdaterService.GetCurrentVersion().ToString(3)
            );
            GlobalHotKeyManager.UnregisterAll();

            ThemeService.LoadThemes();
            PopulateThemeComboBox();

            ThemeService.RefreshActiveWindow(this);
        }

        private void LoadSettings()
        {
            ClickSpeedTextBox.Text = _settings.ClickSpeed.ToString(CultureInfo.InvariantCulture);
            GameProcessNameTextBox.Text = _settings.GameProcessName;
            GameProcessNameTextBox.IsReadOnly = !_isGameProcessNameUnlocked;
            UnlockGameProcessNameButton.Visibility = _isGameProcessNameUnlocked ? Visibility.Collapsed : Visibility.Visible;
            GamePathTextBox.Text = _settings.GameExecutablePath;

            RedTeamXTextBox.Text = _settings.RedTeamCoordinates.X.ToString(CultureInfo.InvariantCulture);
            RedTeamYTextBox.Text = _settings.RedTeamCoordinates.Y.ToString(CultureInfo.InvariantCulture);
            BlueTeamXTextBox.Text = _settings.BlueTeamCoordinates.X.ToString(CultureInfo.InvariantCulture);
            BlueTeamYTextBox.Text = _settings.BlueTeamCoordinates.Y.ToString(CultureInfo.InvariantCulture);

            GlobalHotKeyBox.HotKey = _settings.GlobalHotKey;
            RedTeamHotKeyBox.HotKey = _settings.RedTeamHotKey;
            BlueTeamHotKeyBox.HotKey = _settings.BlueTeamHotKey;
            GoldBoxTimerHotKeyBox.HotKey = _settings.GoldBoxTimerHotKey;

            SmartRepairToggleHotKeyBox.HotKey = _settings.SmartRepairToggleHotKey;
            SmartRepairDebugHotKeyBox.HotKey = _settings.SmartRepairDebugHotKey;

            RepairKitKeyBox.HotKey = new HotKey(_settings.PowerupKeys.GetValueOrDefault(PowerupType.RepairKit, Key.D1), ModifierKeys.None);
            DoubleArmorKeyBox.HotKey = new HotKey(_settings.PowerupKeys.GetValueOrDefault(PowerupType.DoubleArmor, Key.D2), ModifierKeys.None);
            DoubleDamageKeyBox.HotKey = new HotKey(_settings.PowerupKeys.GetValueOrDefault(PowerupType.DoubleDamage, Key.D3), ModifierKeys.None);
            SpeedBoostKeyBox.HotKey = new HotKey(_settings.PowerupKeys.GetValueOrDefault(PowerupType.SpeedBoost, Key.D4), ModifierKeys.None);
            MineKeyBox.HotKey = new HotKey(_settings.PowerupKeys.GetValueOrDefault(PowerupType.Mine, Key.D5), ModifierKeys.None);

            TransparentTimerWindowCheckBox.IsChecked = _settings.IsTimerWindowTransparent;

            foreach (ComboBoxItem item in SmartRepairFpsComboBox.Items)
            {
                if (item.Tag.ToString() == _settings.SmartRepairFps.ToString())
                {
                    SmartRepairFpsComboBox.SelectedItem = item;
                    break;
                }
            }

            LanguageComboBox.ItemsSource = LanguageService.GetLanguages();
            LanguageComboBox.SelectedValue = _settings.Language;
        }

        private void PopulateThemeComboBox()
        {
            ThemeComboBox.SelectionChanged -= ThemeComboBox_SelectionChanged;
            ThemeComboBox.Items.Clear();
            DeleteThemeButton.Visibility = Visibility.Collapsed;

            ComboBoxItem lightItem = new() { Content = "Light", Tag = ThemeType.Light };
            ComboBoxItem darkItem = new() { Content = "Dark", Tag = ThemeType.Dark };

            _ = ThemeComboBox.Items.Add(lightItem);
            _ = ThemeComboBox.Items.Add(darkItem);

            foreach (CustomTheme customTheme in ThemeService.LoadedThemes)
            {
                _ = ThemeComboBox.Items.Add(new ComboBoxItem { Content = customTheme.Name, Tag = customTheme });
            }

            if (!string.IsNullOrEmpty(_settings.CustomThemeName))
            {
                ComboBoxItem? customThemeMatch = ThemeComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(x => x.Content != null && x.Content.ToString() == _settings.CustomThemeName);

                if (customThemeMatch != null)
                {
                    ThemeComboBox.SelectedItem = customThemeMatch;
                    DeleteThemeButton.Visibility = Visibility.Visible;
                }
                else
                {
                    ThemeComboBox.SelectedItem = _settings.Theme == ThemeType.Light ? lightItem : darkItem;
                }
            }
            else
            {
                ThemeComboBox.SelectedItem = _settings.Theme == ThemeType.Light ? lightItem : darkItem;
            }

            ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || LanguageComboBox.SelectedValue is not string selectedLangCode)
            {
                return;
            }

            _settings.Language = selectedLangCode;
            LanguageService.SetLanguage(selectedLangCode);
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || ThemeComboBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                return;
            }

            if (selectedItem.Tag is ThemeType standardTheme)
            {
                ThemeService.ClearThemeOverrides();
                App.ApplyTheme(standardTheme);

                _settings.Theme = standardTheme;
                _settings.CustomThemeName = null;
                DeleteThemeButton.Visibility = Visibility.Collapsed;
            }
            else if (selectedItem.Tag is CustomTheme customTheme)
            {
                ThemeService.ApplyTheme(customTheme);

                _settings.Theme = ThemeType.Dark;
                _settings.CustomThemeName = customTheme.Name;
                DeleteThemeButton.Visibility = Visibility.Visible;
            }
        }

        private void CreateThemeButton_Click(object sender, RoutedEventArgs e)
        {
            ThemeCreatorWindow creator = new() { Owner = this };
            if (creator.ShowDialog() == true)
            {
                ThemeService.LoadThemes();
                PopulateThemeComboBox();

                if (ThemeComboBox.Items.Count > 0)
                {
                    ThemeComboBox.SelectedIndex = ThemeComboBox.Items.Count - 1;
                }
            }
        }

        private async void DeleteThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is CustomTheme themeToDelete)
            {
                MessageBox uiMessageBox = new()
                {
                    Title = "Delete Theme",
                    Content = $"Are you sure you want to delete '{themeToDelete.Name}'?",
                    PrimaryButtonText = "Yes, Delete",
                    CloseButtonText = "Cancel"
                };

                MessageBoxResult result = await uiMessageBox.ShowDialogAsync();

                if (result == MessageBoxResult.Primary)
                {
                    ThemeService.DeleteTheme(themeToDelete);
                    _settings.CustomThemeName = null;
                    ThemeService.ClearThemeOverrides();
                    PopulateThemeComboBox();
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(this, SaveButton);

            if (HasDuplicateHotkeys(out string duplicateMessage))
            {
                MessageBox uiMessageBox = new() { Title = "Duplicate Hotkeys", Content = duplicateMessage, CloseButtonText = "OK" };
                _ = await uiMessageBox.ShowDialogAsync();
                return;
            }

            if (!double.TryParse(ClickSpeedTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double clickSpeed) || clickSpeed < 1)
            {
                return;
            }

            _settings.ClickSpeed = clickSpeed;

            if (SmartRepairFpsComboBox.SelectedItem is ComboBoxItem selectedFpsItem && int.TryParse(selectedFpsItem.Tag.ToString(), out int fps))
            {
                _settings.SmartRepairFps = fps;
            }

            if (LanguageComboBox.SelectedValue is string langCode)
            {
                _settings.Language = langCode;
                LanguageService.SetLanguage(langCode);
            }

            _settings.GameProcessName = !string.IsNullOrWhiteSpace(GameProcessNameTextBox.Text) ? GameProcessNameTextBox.Text.Trim() : "ProTanki";
            _settings.GameExecutablePath = GamePathTextBox.Text;

            if (double.TryParse(RedTeamXTextBox.Text, out double rX) && double.TryParse(RedTeamYTextBox.Text, out double rY))
            {
                _settings.RedTeamCoordinates = new Point(rX, rY);
            }

            if (double.TryParse(BlueTeamXTextBox.Text, out double bX) && double.TryParse(BlueTeamYTextBox.Text, out double bY))
            {
                _settings.BlueTeamCoordinates = new Point(bX, bY);
            }

            _settings.GlobalHotKey = GlobalHotKeyBox.HotKey;
            _settings.RedTeamHotKey = RedTeamHotKeyBox.HotKey;
            _settings.BlueTeamHotKey = BlueTeamHotKeyBox.HotKey;
            _settings.GoldBoxTimerHotKey = GoldBoxTimerHotKeyBox.HotKey;

            _settings.SmartRepairToggleHotKey = SmartRepairToggleHotKeyBox.HotKey;
            _settings.SmartRepairDebugHotKey = SmartRepairDebugHotKeyBox.HotKey;

            _settings.PowerupKeys[PowerupType.RepairKit] = RepairKitKeyBox.HotKey.Key;
            _settings.PowerupKeys[PowerupType.DoubleArmor] = DoubleArmorKeyBox.HotKey.Key;
            _settings.PowerupKeys[PowerupType.DoubleDamage] = DoubleDamageKeyBox.HotKey.Key;
            _settings.PowerupKeys[PowerupType.SpeedBoost] = SpeedBoostKeyBox.HotKey.Key;
            _settings.PowerupKeys[PowerupType.Mine] = MineKeyBox.HotKey.Key;

            _settings.IsTimerWindowTransparent = TransparentTimerWindowCheckBox.IsChecked ?? false;

            CustomTheme? themeToApply = null;
            if (ThemeComboBox.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag is ThemeType t)
                {
                    _settings.Theme = t;
                    _settings.CustomThemeName = null;
                }
                else if (item.Tag is CustomTheme c)
                {
                    _settings.CustomThemeName = c.Name;
                    _settings.Theme = ThemeType.Dark;
                    themeToApply = c;
                }
            }

            _settings.Save();

            if (App.Settings != null)
            {
                App.Settings.ClickSpeed = _settings.ClickSpeed;
                App.Settings.GameProcessName = _settings.GameProcessName;
                App.Settings.GameExecutablePath = _settings.GameExecutablePath;
                App.Settings.RedTeamCoordinates = _settings.RedTeamCoordinates;
                App.Settings.BlueTeamCoordinates = _settings.BlueTeamCoordinates;
                App.Settings.GoldBoxColor = _settings.GoldBoxColor;
                App.Settings.GlobalHotKey = _settings.GlobalHotKey;
                App.Settings.RedTeamHotKey = _settings.RedTeamHotKey;
                App.Settings.BlueTeamHotKey = _settings.BlueTeamHotKey;
                App.Settings.GoldBoxTimerHotKey = _settings.GoldBoxTimerHotKey;
                App.Settings.SmartRepairToggleHotKey = _settings.SmartRepairToggleHotKey;
                App.Settings.SmartRepairDebugHotKey = _settings.SmartRepairDebugHotKey;
                App.Settings.PowerupKeys = _settings.PowerupKeys;
                App.Settings.IsTimerWindowTransparent = _settings.IsTimerWindowTransparent;
                App.Settings.Theme = _settings.Theme;
                App.Settings.CustomThemeName = _settings.CustomThemeName;
                App.Settings.SmartRepairFps = _settings.SmartRepairFps;
            }

            RegisterSettingsHotkeys();

            if (themeToApply != null)
            {
                ThemeService.ApplyTheme(themeToApply);
            }
            else if (_settings.CustomThemeName == null)
            {
                ThemeService.ClearThemeOverrides();
                App.ApplyTheme(_settings.Theme);
            }

            Close();
        }

        private bool HasDuplicateHotkeys(out string message)
        {
            message = string.Empty;

            List<(string Name, HotKey Key)> hotkeys =
            [
                ("Global Toggle", GlobalHotKeyBox.HotKey),
                ("Red Team", RedTeamHotKeyBox.HotKey),
                ("Blue Team", BlueTeamHotKeyBox.HotKey),
                ("Gold Box Timer", GoldBoxTimerHotKeyBox.HotKey),
                ("Repair Kit", RepairKitKeyBox.HotKey),
                ("Double Armor", DoubleArmorKeyBox.HotKey),
                ("Double Damage", DoubleDamageKeyBox.HotKey),
                ("Speed Boost", SpeedBoostKeyBox.HotKey),
                ("Mine", MineKeyBox.HotKey),
                ("Smart Repair Toggle", SmartRepairToggleHotKeyBox.HotKey),
                ("Smart Repair Debug", SmartRepairDebugHotKeyBox.HotKey)
            ];

            List<(string Name, HotKey Key)> validKeys = [.. hotkeys.Where(x => x.Key != null && !x.Key.IsEmpty)];

            var duplicates = validKeys
                .GroupBy(x => x.Key)
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    KeyName = g.Key.ToString(),
                    Conflicts = string.Join(", ", g.Select(i => i.Name))
                })
                .ToList();

            if (duplicates.Count != 0)
            {
                message = "The following hotkeys are assigned to multiple actions:\n\n" +
                          string.Join("\n", duplicates.Select(d => $"{d.KeyName}: {d.Conflicts}"));
                return true;
            }

            return false;
        }

        private void RegisterSettingsHotkeys()
        {
            GlobalHotKeyManager.UnregisterAll();

            _ = GlobalHotKeyManager.Register(_settings.GlobalHotKey);
            _ = GlobalHotKeyManager.Register(_settings.RedTeamHotKey);
            _ = GlobalHotKeyManager.Register(_settings.BlueTeamHotKey);
            _ = GlobalHotKeyManager.Register(_settings.GoldBoxTimerHotKey);

            foreach (KeyValuePair<PowerupType, Key> entry in _settings.PowerupKeys)
            {
                _ = GlobalHotKeyManager.Register(new HotKey(entry.Value, ModifierKeys.None));
            }
        }

        private void BrowseGamePathButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Select Game Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                GamePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void UnlockGameProcessNameButton_Click(object sender, RoutedEventArgs e)
        {
            AdminPasswordDialog adminPasswordDialog = new() { Owner = this };
            if (adminPasswordDialog.ShowDialog() == true)
            {
                _isGameProcessNameUnlocked = true;
                GameProcessNameTextBox.IsReadOnly = false;
                _ = GameProcessNameTextBox.Focus();
                UnlockGameProcessNameButton.Visibility = Visibility.Collapsed;
            }
        }

        private void PickRedTeamButton_Click(object sender, RoutedEventArgs e)
        {
            CoordinatePickerWindow picker = new()
            {
                Owner = this
            };

            bool? result = picker.ShowDialog();

            _ = Activate();
            _ = Focus();

            if (result == true)
            {
                Point finalPoint = picker.SelectedPoint;
                Point? clientPoint = ConvertScreenToClient(picker.SelectedPoint);

                if (clientPoint.HasValue)
                {
                    finalPoint = clientPoint.Value;
                }
                else
                {
                    _ = System.Windows.MessageBox.Show("Game process not found. Coordinates saved as absolute screen coordinates. Ensure the game is running for accurate relative positioning.", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }

                RedTeamXTextBox.Text = finalPoint.X.ToString(CultureInfo.InvariantCulture);
                RedTeamYTextBox.Text = finalPoint.Y.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void PickBlueTeamButton_Click(object sender, RoutedEventArgs e)
        {
            CoordinatePickerWindow picker = new()
            {
                Owner = this
            };

            bool? result = picker.ShowDialog();

            _ = Activate();
            _ = Focus();

            if (result == true)
            {
                Point finalPoint = picker.SelectedPoint;
                Point? clientPoint = ConvertScreenToClient(picker.SelectedPoint);

                if (clientPoint.HasValue)
                {
                    finalPoint = clientPoint.Value;
                }
                else
                {
                    _ = System.Windows.MessageBox.Show("Game process not found. Coordinates saved as absolute screen coordinates. Ensure the game is running for accurate relative positioning.", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }

                BlueTeamXTextBox.Text = finalPoint.X.ToString(CultureInfo.InvariantCulture);
                BlueTeamYTextBox.Text = finalPoint.Y.ToString(CultureInfo.InvariantCulture);
            }
        }

        private Point? ConvertScreenToClient(Point screenPoint)
        {
            Process? gameProcess = Process.GetProcessesByName(_settings.GameProcessName).FirstOrDefault();
            if (gameProcess == null || gameProcess.MainWindowHandle == IntPtr.Zero)
            {
                return null;
            }

            NativeMethods.POINT pt = new() { X = (int)screenPoint.X, Y = (int)screenPoint.Y };
            _ = NativeMethods.ScreenToClient(gameProcess.MainWindowHandle, ref pt);
            return new Point(pt.X, pt.Y);
        }

        private void LogsButton_Click(object sender, RoutedEventArgs e)
        {
            LogViewerWindow logViewer = new() { Owner = this };
            _ = logViewer.ShowDialog();
        }

        private void ViewReportsButton_Click(object sender, RoutedEventArgs e)
        {
            ReportViewerWindow reportViewerWindow = new() { Owner = this };
            _ = reportViewerWindow.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = (string)Application.Current.Resources["Settings_CheckingUpdates"];
            try
            {
                (Version? latestVersion, string? releaseNotes) = await _updaterService.GetLatestVersionAsync();
                LatestVersionTextBlock.Text = string.Format(
                    (string)Application.Current.Resources["Settings_LatestVersion"],
                    latestVersion?.ToString(3)
                );

                if (latestVersion != null && latestVersion > UpdaterService.GetCurrentVersion())
                {
                    StatusTextBlock.Text = (string)Application.Current.Resources["Settings_UpdateAvailable"];
                    UpdateNowButton.Visibility = Visibility.Visible;
                }
                else
                {
                    StatusTextBlock.Text = (string)Application.Current.Resources["Settings_UpToDate"];
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = (string)Application.Current.Resources["Settings_ErrorCheckingUpdates"];
                LogService.LogError($"Error checking for updates: {ex}");
            }
        }

        private async void UpdateNowButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateNowButton.IsEnabled = false;
            CheckForUpdatesButton.IsEnabled = false;
            StatusTextBlock.Text = (string)Application.Current.Resources["Settings_DownloadingUpdate"];
            DownloadProgressBar.Visibility = Visibility.Visible;

            try
            {
                string? installerPath = await _updaterService.DownloadUpdateAsync((bytesReceived, totalBytes) =>
                {
                    _ = Dispatcher.BeginInvoke(() =>
                    {
                        DownloadProgressBar.Value = (double)bytesReceived / totalBytes * 100;
                    });
                });

                if (!string.IsNullOrEmpty(installerPath))
                {
                    StatusTextBlock.Text = (string)Application.Current.Resources["Settings_UpdateDownloaded"];
                    UpdaterService.InstallUpdate(installerPath);
                }
                else
                {
                    StatusTextBlock.Text = (string)Application.Current.Resources["Settings_DownloadFailed"];
                    UpdateNowButton.IsEnabled = true;
                    CheckForUpdatesButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = string.Format(
                    (string)Application.Current.Resources["Settings_Error"],
                    ex.Message
                );
                UpdateNowButton.IsEnabled = true;
                CheckForUpdatesButton.IsEnabled = true;
                DownloadProgressBar.Visibility = Visibility.Collapsed;
            }
        }
        private void Hyperlink_RequestNavigate(object sender, Navigation.RequestNavigateEventArgs e)
        {
            _ = Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void BtnCreateNewLanguage_Click(object sender, RoutedEventArgs e)
        {
            LanguageEditorWindow editor = new() { Owner = this };
            _ = editor.ShowDialog();
        }

        private void BtnEditLanguage_Click(object sender, RoutedEventArgs e)
        {
            LanguagePickerWindow picker = new(true) { Owner = this };
            if (picker.ShowDialog() == true && !string.IsNullOrEmpty(picker.SelectedLanguageCode))
            {
                LanguageEditorWindow editor = new(picker.SelectedLanguageCode) { Owner = this };
                _ = editor.ShowDialog();
            }
        }
    }
}
