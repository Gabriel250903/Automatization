using Automatization.Hotkeys;
using Automatization.Listeners;
using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using Automatization.UI;
using Automatization.Utils;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;

namespace Automatization
{
    public partial class MainWindow : FluentWindow
    {
        private DispatcherTimer _gameCheckTimer = null!;
        private readonly PowerupUtils? _powerupUtils;
        private Process? _gameProcess;
        private string? _gameExecutablePath;
        private AppSettings _settings;
        private ClickerService? _clickerService;
        private Guid? _redClickerId;
        private Guid? _blueClickerId;

        private readonly Dictionary<string, Action> _hotkeyActions = [];
        private KeyboardListener? _keyboardListener;

        private readonly List<TimerWindow> _activeTimerWindows = [];
        private SmartRepairKitWindow? _smartRepairWindow;
        private AutoGoldBoxService? _autoGoldBoxService;

        private bool _arePowerupsPausedForChat = false;
        private readonly List<PowerupType> _activePowerups = [];
        private string _currentStatusKey = "Status_Ready";


        #region Win32
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private static IntPtr MakeLParam(int x, int y)
        {
            return (y << 16) | (x & 0xFFFF);
        }
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_RBUTTONDOWN = 0x0204;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_MBUTTONDOWN = 0x0207;
        private const uint WM_MBUTTONUP = 0x0208;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            LogService.LogInfo("Initializing main window.");
            _settings = AppSettings.Load();
            _powerupUtils = new PowerupUtils(_settings);
            DataContext = _powerupUtils;

            ClickTypeComboBox.ItemsSource = Enum.GetValues(typeof(ClickType));
            ClickTypeComboBox.SelectedIndex = 0;

            CheckGameInstallation();
            InitializeGameCheckTimer();

            SourceInitialized += OnSourceInitialized;

            DisableAutomation();
            _ = GameCheckAsync();

            PauseHotkeysCheckBox.IsChecked = GlobalHotKeyManager.IsPaused;
            PauseHotkeysCheckBox.IsEnabled = true;
            Status = "Status_Ready";
            LanguageService.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            UpdateTranslatedStatus();
        }

        private void UpdateTranslatedStatus()
        {
            Status = _currentStatusKey;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            LogService.LogInfo("Main window source initialized.");

            InitializeTeamClickers();
            _powerupUtils?.Initialize();

            _autoGoldBoxService = new AutoGoldBoxService();
            _autoGoldBoxService.OnTriggered += AutoGoldBoxService_OnTriggered;

            AutoGoldBoxCheckBox.IsChecked = _settings.EnableAutoGoldBox;

            if (_settings.EnableAutoGoldBox)
            {
                _autoGoldBoxService.Start();
            }

            GlobalHotKeyManager.Initialize();
            GlobalHotKeyManager.HotKeyPressed += OnHotKeyPressed;
            RegisterHotkeysFromSettings();

            _keyboardListener = new KeyboardListener();
            _keyboardListener.KeyDown += KeyboardListener_OnKeyPressed;

            _hotkeyActions["ToggleAll"] = () => _powerupUtils?.ToggleAll();
            _hotkeyActions["RedTeam"] = () => RedTeamButton_Click(this, null);
            _hotkeyActions["BlueTeam"] = () => BlueTeamButton_Click(this, null);
            _hotkeyActions["StartTimer"] = StartGoldBoxTimer;

            _hotkeyActions["SmartRepairToggle"] = ToggleSmartRepair;
            _hotkeyActions["SmartRepairDebug"] = DebugSmartRepair;
        }

        private void AutoGoldBoxService_OnTriggered()
        {
            Dispatcher.Invoke(() =>
            {
                System.Media.SystemSounds.Asterisk.Play();
                StartGoldBoxTimer();
            });
        }

        private void ToggleSmartRepair()
        {
            if (_smartRepairWindow == null || !_smartRepairWindow.IsLoaded)
            {
                SmartRepairButton_Click(this, null);

                _smartRepairWindow?.ToggleMonitoring();
            }
            else
            {
                _smartRepairWindow.ToggleMonitoring();
            }
        }

        private void DebugSmartRepair()
        {
            if (_smartRepairWindow != null && _smartRepairWindow.IsLoaded)
            {
                _smartRepairWindow.SaveDebugSnapshot();
            }
        }

        private bool KeyboardListener_OnKeyPressed(Key e)
        {
            if (_gameProcess != null && WindowUtils.IsGameWindowInForeground(_gameProcess))
            {
                if (e == Key.Enter && !_arePowerupsPausedForChat)
                {
                    if (_powerupUtils != null)
                    {
                        _activePowerups.Clear();
                        foreach (ViewModels.PowerupViewModel powerup in _powerupUtils.Powerups)
                        {
                            if (powerup.IsActive)
                            {
                                _activePowerups.Add(powerup.PowerupType);
                            }
                        }

                        _powerupUtils.StopAll();
                    }

                    _arePowerupsPausedForChat = true;
                    LogService.LogInfo("Chat opened: Paused powerups.");
                }
                else if ((e == Key.Enter || e == Key.Escape) && _arePowerupsPausedForChat)
                {
                    _arePowerupsPausedForChat = false;
                    List<PowerupType> powerupsToResume = [.. _activePowerups];
                    _activePowerups.Clear();

                    _ = Dispatcher.InvokeAsync(async () =>
                    {
                        await Task.Delay(500);

                        if (!_arePowerupsPausedForChat && _powerupUtils != null)
                        {
                            foreach (PowerupType type in powerupsToResume)
                            {
                                ViewModels.PowerupViewModel? vm = _powerupUtils.Powerups.FirstOrDefault(x => x.PowerupType == type);
                                if (vm != null)
                                {
                                    vm.IsActive = true;
                                }
                            }
                        }
                    });

                    LogService.LogInfo($"Chat closed: Resuming powerups in 500ms.");
                }

                if (!_arePowerupsPausedForChat && !GlobalHotKeyManager.IsPaused && _powerupUtils != null)
                {
                    KeyValuePair<PowerupType, Key> powerupMapping = _settings.PowerupKeys.FirstOrDefault(kvp => kvp.Value == e);

                    if (powerupMapping.Key != default)
                    {
                        _powerupUtils.UsePowerup(powerupMapping.Key);
                        LogService.LogInfo($"Detected powerup key {e}, triggering {powerupMapping.Key}");
                        return true;
                    }
                }
            }

            return false;
        }

        private void OnHotKeyPressed(HotKey hotKey, Process? gameProcess)
        {
            LogService.LogInfo($"Hotkey pressed: {hotKey}");

            if (gameProcess != null)
            {
                if (_gameProcess == null || _gameProcess.Id != gameProcess.Id)
                {
                    _gameProcess = gameProcess;
                    EnableAutomation();
                }

                if (_powerupUtils != null)
                {
                    _powerupUtils.GameProcess = gameProcess;
                }
            }

            string? actionEntry = _settings.GetActionForHotKey(hotKey);

            if (actionEntry != null && _hotkeyActions.TryGetValue(actionEntry, out Action? action))
            {
                action.Invoke();
                return;
            }

            KeyValuePair<PowerupType, Key> powerupMapping = _settings.PowerupKeys.FirstOrDefault(kvp => kvp.Value == hotKey.Key);

            if (powerupMapping.Key != default)
            {
                _powerupUtils?.UsePowerup(powerupMapping.Key);

                LogService.LogInfo($"Used powerup {powerupMapping.Key} via hotkey.");
                return;
            }
        }

        public void RegisterHotkeysFromSettings()
        {
            GlobalHotKeyManager.UnregisterAll();
            _ = GlobalHotKeyManager.Register(_settings.GlobalHotKey);
            _ = GlobalHotKeyManager.Register(_settings.RedTeamHotKey);
            _ = GlobalHotKeyManager.Register(_settings.BlueTeamHotKey);
            _ = GlobalHotKeyManager.Register(_settings.GoldBoxTimerHotKey);
            _ = GlobalHotKeyManager.Register(_settings.SmartRepairToggleHotKey);
            _ = GlobalHotKeyManager.Register(_settings.SmartRepairDebugHotKey);
        }

        private void AutoGoldBoxCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _settings.EnableAutoGoldBox = true;
            _settings.Save();

            if (_autoGoldBoxService != null)
            {
                _autoGoldBoxService.IsEnabled = true;
                _autoGoldBoxService.Start();
            }

            LogService.LogInfo("Auto Gold Box enabled via Main Window.");
        }

        private void AutoGoldBoxCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _settings.EnableAutoGoldBox = false;
            _settings.Save();

            if (_autoGoldBoxService != null)
            {
                _autoGoldBoxService.IsEnabled = false;
                _autoGoldBoxService.Stop();
            }

            LogService.LogInfo("Auto Gold Box disabled via Main Window.");
        }

        private void StartGoldBoxTimer()
        {
            TimerWindow newTimer = new(startTimer: true);

            if (_activeTimerWindows.Count != 0)
            {
                TimerWindow lastTimer = _activeTimerWindows.Last();

                newTimer.Left = lastTimer.Left + lastTimer.ActualWidth + 5;
                newTimer.Top = lastTimer.Top;
            }

            newTimer.Closed += (sender, args) =>
            {
                if (sender is TimerWindow closedTimer)
                {
                    _ = _activeTimerWindows.Remove(closedTimer);
                }
            };

            _activeTimerWindows.Add(newTimer);
            newTimer.Show();
        }

        #region Team Auto-Clickers
        private void InitializeTeamClickers()
        {
            _clickerService = new ClickerService(_settings);
        }

        private NativeMethods.POINT GetScreenCoordinates(Point clientPt)
        {
            NativeMethods.POINT pt = new() { X = (int)clientPt.X, Y = (int)clientPt.Y };
            if (_gameProcess != null && _gameProcess.MainWindowHandle != IntPtr.Zero)
            {
                _ = NativeMethods.ClientToScreen(_gameProcess.MainWindowHandle, ref pt);
            }
            return pt;
        }

        private void ClickTeamButton(IntPtr windowHandle, int x, int y, ClickType clickType)
        {
            _ = Dispatcher.BeginInvoke(() => LogService.LogInfo($"Clicking team button at ({x}, {y}) with {clickType} click."));

            IntPtr lParam = MakeLParam(x, y);

            switch (clickType)
            {
                case ClickType.Left:
                    _ = PostMessage(windowHandle, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
                    _ = PostMessage(windowHandle, WM_LBUTTONUP, IntPtr.Zero, lParam);
                    break;
                case ClickType.Right:
                    _ = PostMessage(windowHandle, WM_RBUTTONDOWN, IntPtr.Zero, lParam);
                    _ = PostMessage(windowHandle, WM_RBUTTONUP, IntPtr.Zero, lParam);
                    break;
                case ClickType.Middle:
                    _ = PostMessage(windowHandle, WM_MBUTTONDOWN, IntPtr.Zero, lParam);
                    _ = PostMessage(windowHandle, WM_MBUTTONUP, IntPtr.Zero, lParam);
                    break;
                case ClickType.DoubleClick:
                    _ = PostMessage(windowHandle, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
                    _ = PostMessage(windowHandle, WM_LBUTTONUP, IntPtr.Zero, lParam);
                    _ = PostMessage(windowHandle, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
                    _ = PostMessage(windowHandle, WM_LBUTTONUP, IntPtr.Zero, lParam);
                    break;
            }
        }

        private void RedTeamButton_Click(object sender, RoutedEventArgs? e)
        {
            if (_redClickerId.HasValue)
            {
                _clickerService?.Unregister(_redClickerId.Value);
                _redClickerId = null;

                Dispatcher.Invoke(() =>
                {
                    RedTeamButton.Content = (string)Application.Current.Resources["Team_AutoRed"];
                    Status = "Status_RedTeamStopped";
                });

                LogService.LogInfo("Red Team auto-clicker stopped.");
            }
            else
            {
                ClickType clickType = (ClickType)ClickTypeComboBox.SelectedItem;

                _redClickerId = _clickerService?.Register(
                    (handle, ct) =>
                    {
                        ClickTeamButton(handle, (int)_settings.RedTeamCoordinates.X, (int)_settings.RedTeamCoordinates.Y, ct);
                    },
                    clickType,
                    _settings.GameProcessName
                );

                Dispatcher.Invoke(() =>
                {
                    RedTeamButton.Content = (string)Application.Current.Resources["Team_StopRed"];
                    Status = "Status_RedTeamStarted";
                });

                LogService.LogInfo("Red Team auto-clicker started.");
            }
        }

        private void BlueTeamButton_Click(object sender, RoutedEventArgs? e)
        {
            if (_blueClickerId.HasValue)
            {
                _clickerService?.Unregister(_blueClickerId.Value);

                _blueClickerId = null;

                Dispatcher.Invoke(() =>
                {
                    BlueTeamButton.Content = (string)Application.Current.Resources["Team_AutoBlue"];
                    Status = "Status_BlueTeamStopped";
                });

                LogService.LogInfo("Blue Team auto-clicker stopped.");
            }
            else
            {
                ClickType clickType = (ClickType)ClickTypeComboBox.SelectedItem;

                _blueClickerId = _clickerService?.Register(
                    (handle, ct) =>
                    {
                        ClickTeamButton(handle, (int)_settings.BlueTeamCoordinates.X, (int)_settings.BlueTeamCoordinates.Y, ct);
                    },
                    clickType,
                    _settings.GameProcessName
                );

                Dispatcher.Invoke(() =>
                {
                    BlueTeamButton.Content = (string)Application.Current.Resources["Team_StopBlue"];
                    Status = "Status_BlueTeamStarted";
                });

                LogService.LogInfo("Blue Team auto-clicker started.");
            }
        }

        #endregion

        #region Game Check Timer
        private void InitializeGameCheckTimer()
        {
            _gameCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _gameCheckTimer.Tick += async (_, _) => await GameCheckAsync();
            _gameCheckTimer.Start();
        }

        private async Task GameCheckAsync()
        {
            Process? game = await Task.Run(() => Process.GetProcessesByName(_settings.GameProcessName).FirstOrDefault());

            if (game != null)
            {
                if (_gameProcess == null || _gameProcess.Id != game.Id)
                {
                    _gameProcess = game;
                    if (_powerupUtils != null)
                    {
                        _powerupUtils.GameProcess = _gameProcess;
                    }

                    EnableAutomation();
                    LogService.LogInfo("Game process found.");
                }
            }
            else
            {
                if (_gameProcess != null)
                {
                    _gameProcess = null;
                    if (_powerupUtils != null)
                    {
                        _powerupUtils.GameProcess = null;
                    }

                    DisableAutomation();
                    LogService.LogInfo("Game process lost.");
                }
            }
        }
        #endregion

        #region Automation UI
        private void EnableAutomation()
        {
            PowerupsGroup.Visibility = Visibility.Visible;
            SmartFeaturesGroup.Visibility = Visibility.Visible;
            LaunchButton.Visibility = Visibility.Collapsed;
            Status = "Status_GameRunning";

            if (_settings.EnableAutoGoldBox)
            {
                _autoGoldBoxService?.Start();
            }

            LogService.LogInfo("Automation enabled.");
        }

        private void DisableAutomation()
        {
            _powerupUtils?.StopAll();
            _autoGoldBoxService?.Stop();
            PowerupsGroup.Visibility = Visibility.Collapsed;
            SmartFeaturesGroup.Visibility = Visibility.Collapsed;
            LaunchButton.Visibility = Visibility.Visible;
            Status = "Status_GameNotRunning";

            LogService.LogInfo("Automation disabled.");
        }
        #endregion

        #region Game Launch / Installation
        private void CheckGameInstallation()
        {
            if (!string.IsNullOrEmpty(_settings.GameExecutablePath) && File.Exists(Path.Combine(_settings.GameExecutablePath, _settings.GameProcessName + ".exe")))
            {
                _gameExecutablePath = Path.Combine(_settings.GameExecutablePath, _settings.GameProcessName + ".exe");
            }
            else
            {
                string? found = FindExecutableFromUninstallRegistry() ?? SearchCommonInstallDirectories();

                if (found != null)
                {
                    _gameExecutablePath = found;
                    _settings.GameExecutablePath = Path.GetDirectoryName(found) ?? string.Empty;
                    _settings.Save();
                }
            }
        }

        private static string? FindExecutableFromUninstallRegistry()
        {
            string gameProcessName = AppSettings.Load().GameProcessName;
            string[] registryPaths =
            [
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            ];

            foreach (string path in registryPaths)
            {
                foreach (RegistryKey baseKey in new[] { Microsoft.Win32.Registry.LocalMachine, Microsoft.Win32.Registry.CurrentUser })
                {
                    using RegistryKey? key = baseKey.OpenSubKey(path);
                    if (key == null)
                    {
                        continue;
                    }

                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        using RegistryKey? subKey = key.OpenSubKey(subKeyName);
                        if (subKey == null)
                        {
                            continue;
                        }

                        string? displayName = subKey.GetValue("DisplayName")?.ToString();
                        if (displayName != null && displayName.Contains(gameProcessName + " Online", StringComparison.OrdinalIgnoreCase))
                        {
                            string? installLocation = subKey.GetValue("InstallLocation")?.ToString();
                            if (!string.IsNullOrEmpty(installLocation))
                            {
                                string executablePath = Path.Combine(installLocation, gameProcessName + ".exe");
                                if (File.Exists(executablePath))
                                {
                                    return executablePath;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static string? SearchCommonInstallDirectories()
        {
            string gameProcessName = AppSettings.Load().GameProcessName;

            List<string> commonPaths =
            [
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            ];

            foreach (string basePath in commonPaths)
            {
                string directPath = Path.Combine(basePath, gameProcessName + ".exe");
                if (File.Exists(directPath))
                {
                    return directPath;
                }

                string subDirPath = Path.Combine(basePath, gameProcessName + " Online", gameProcessName + ".exe");
                if (File.Exists(subDirPath))
                {
                    return subDirPath;
                }

                subDirPath = Path.Combine(basePath, gameProcessName, gameProcessName + ".exe");
                if (File.Exists(subDirPath))
                {
                    return subDirPath;
                }
            }

            return null;
        }

        private void LaunchGame()
        {
            try
            {
                if (!File.Exists(_gameExecutablePath))
                {
                    Status = "Status_GameNotFound";
                    LogService.LogWarning("Game executable not found.");

                    OpenFileDialog dlg = new()
                    {
                        Filter = "Executable files (*.exe)|*.exe",
                        Title = $"Select {_settings.GameProcessName} Online executable"
                    };

                    if (dlg.ShowDialog() == true)
                    {
                        _gameExecutablePath = dlg.FileName;
                        _settings.GameExecutablePath = Path.GetDirectoryName(dlg.FileName);
                        _settings.Save();

                        LogService.LogInfo($"Game executable path set to: {_gameExecutablePath}");
                    }
                    else
                    {
                        return;
                    }
                }

                LogService.LogInfo("Launching game.");

                _gameProcess = Process.Start(new ProcessStartInfo { FileName = _gameExecutablePath, UseShellExecute = true });

                if (_gameProcess != null)
                {
                    EnableAutomation();
                }
            }
            catch (Exception ex)
            {
                Status = "Status_LaunchError";
                LogService.LogError("Error launching game.", ex);
            }
        }
        #endregion

        #region UI Event Handlers

        private string Status
        {
            set
            {
                _currentStatusKey = value;
                _ = Dispatcher.Invoke(() => StatusText.Text = (string)Application.Current.Resources[_currentStatusKey]);
            }
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.LogInfo("Launch button clicked.");
            LaunchGame();
        }

        private void DiscountCalculatorButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.LogInfo("Opening Discount Calculator.");
            DiscountCalculatorWindow wnd = new() { Owner = this };
            wnd.Show();
        }

        private void SmartRepairButton_Click(object sender, RoutedEventArgs? e)
        {
            LogService.LogInfo("Opening Smart Repair Kit.");

            if (_smartRepairWindow == null || !_smartRepairWindow.IsLoaded)
            {
                _smartRepairWindow = new SmartRepairKitWindow();
                _smartRepairWindow.Closed += (s, args) => _smartRepairWindow = null;
                _smartRepairWindow.Show();
            }
            else
            {
                _ = _smartRepairWindow.Activate();
            }
        }

        private void RoadmapButton_Click(object sender, RoutedEventArgs e)
        {
            RoadmapWindow wnd = new() { Owner = this };
            wnd.Show();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.LogInfo("Settings button clicked.");

            SettingsWindow wnd = new() { Owner = this };
            _ = wnd.ShowDialog();

            _settings = AppSettings.Load();

            if (_clickerService != null)
            {
                _clickerService.ClickSpeed = _settings.ClickSpeed;
            }

            if (_powerupUtils != null)
            {
                _powerupUtils.Settings = _settings;
                _powerupUtils.Initialize();
            }

            if (_autoGoldBoxService != null)
            {
                if (_settings.EnableAutoGoldBox && _gameProcess != null)
                {
                    _autoGoldBoxService.Start();
                }
                else
                {
                    _autoGoldBoxService.Stop();
                }
            }

            _ = GameCheckAsync();

            if (_settings.CustomThemeName == null)
            {
                App.ApplyTheme(_settings.Theme);
            }

            RegisterHotkeysFromSettings();
        }

        private void PauseHotkeysCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GlobalHotKeyManager.IsPaused = true;
            LogService.LogInfo("Hotkeys paused by checkbox.");
        }

        private void PauseHotkeysCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalHotKeyManager.IsPaused = false;
            LogService.LogInfo("Hotkeys resumed by checkbox.");
        }

        private void ShowDebugButton_Click(object sender, RoutedEventArgs e)
        {
            AdminPasswordDialog passwordDialog = new() { Owner = this };
            if (passwordDialog.ShowDialog() == true)
            {
                DebugGroup.Visibility = Visibility.Visible;
            }
        }

        private void SimulateGoldBox_Click(object sender, RoutedEventArgs e)
        {
            if (_autoGoldBoxService != null)
            {
                _autoGoldBoxService.SimulateTrigger();
            }
            else
            {
                Status = "Status_AutoGoldBoxNotInit";
            }
        }

        private void TestImageDetection_Click(object sender, RoutedEventArgs e)
        {
            if (_autoGoldBoxService == null)
            {
                LogService.LogError("Auto Gold Box Service is not initialized.");
                return;
            }

            OpenFileDialog dlg = new()
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
                Title = "Select Screenshot for Detection Test"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using Bitmap bmp = new(dlg.FileName);

                    string result = _autoGoldBoxService.TestDetection(bmp);
                    _ = MessageBox.Show(result, "Detection Test Result", MessageBoxButton.OK, result.Contains("detected!") ? MessageBoxImage.Information : MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(string.Format("Error loading image: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion

        protected override void OnClosed(EventArgs e)
        {
            LogService.LogInfo("Main window closing.");

            _gameCheckTimer?.Stop();
            _powerupUtils?.StopAll();
            _clickerService?.Dispose();
            _autoGoldBoxService?.Dispose();
            GlobalHotKeyManager.Shutdown();
            _keyboardListener?.Dispose();

            foreach (TimerWindow? timer in _activeTimerWindows.ToList())
            {
                timer.Close();
            }
            _activeTimerWindows.Clear();

            LanguageService.LanguageChanged -= OnLanguageChanged;

            base.OnClosed(e);

        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
        }
    }
}