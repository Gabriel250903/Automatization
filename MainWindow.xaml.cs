using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Automatization.Hotkeys;
using System.Windows.Forms;
using Automatization.Services;
using Automatization.Settings;
using Automatization.Utils;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Automatization.Types;

namespace Automatization
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _gameCheckTimer = null!;
        private PowerupUtils? _powerupUtils;
        private Process? _gameProcess;
        private string? _gameExecutablePath;
        private AppSettings _settings;

        private ClickerService? _clickerService;
        private Guid? _redClickerId;
        private Guid? _blueClickerId;

        private Dictionary<string, Action> _hotkeyActions = new();
        private NotifyIcon? _notifyIcon;

        #region Win32
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        private static IntPtr MakeLParam(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));
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
            _settings = AppSettings.Load();
            _powerupUtils = new PowerupUtils(_settings, PowerupsPanel, ToggleAllButton);

            ClickTypeComboBox.ItemsSource = Enum.GetValues(typeof(ClickType));
            ClickTypeComboBox.SelectedIndex = 0;

            CheckGameInstallation();
            InitializeGameCheckTimer();

            SourceInitialized += OnSourceInitialized;
            ToggleAllButton.Click += (_, _) => _powerupUtils?.ToggleAll();

            DisableAutomation();
            _ = GameCheckAsync();

            PauseHotkeysCheckBox.IsChecked = GlobalHotKeyManager.IsPaused;

            InitializeNotifyIcon();
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            InitializeTeamClickers();
            _powerupUtils?.Initialize();

            GlobalHotKeyManager.Initialize(this);
            GlobalHotKeyManager.HotKeyPressed += OnHotKeyPressed;

            _hotkeyActions["ToggleAll"] = () => _powerupUtils?.ToggleAll();
            _hotkeyActions["RedTeam"] = () => RedTeamButton_Click(RedTeamButton, null);
            _hotkeyActions["BlueTeam"] = () => BlueTeamButton_Click(BlueTeamButton, null);

            RegisterHotkeysFromSettings();
        }

        private void OnHotKeyPressed(HotKey hotKey)
        {
            var actionEntry = _settings.GetActionForHotKey(hotKey);
            if (actionEntry != null && _hotkeyActions.TryGetValue(actionEntry, out var action))
            {
                action.Invoke();
            }
        }

        private void RegisterHotkeysFromSettings()
        {
            GlobalHotKeyManager.Unregister(_settings.GlobalHotKey);
            GlobalHotKeyManager.Unregister(_settings.RedTeamHotKey);
            GlobalHotKeyManager.Unregister(_settings.BlueTeamHotKey);

            GlobalHotKeyManager.Register(_settings.GlobalHotKey);
            GlobalHotKeyManager.Register(_settings.RedTeamHotKey);
            GlobalHotKeyManager.Register(_settings.BlueTeamHotKey);
        }

        #region Team Auto-Clickers

        private void InitializeTeamClickers()
        {
            _clickerService = new ClickerService(_settings);
        }

        private void ClickTeamButton(int x, int y, ClickType clickType)
        {
            if (_gameProcess == null || _gameProcess.MainWindowHandle == IntPtr.Zero)
            {
                return;
            }

            IntPtr lParam = MakeLParam(x, y);

            switch (clickType)
            {
                case ClickType.Left:
                    PostMessage(_gameProcess.MainWindowHandle, WM_LBUTTONDOWN, (IntPtr)1, lParam);
                    PostMessage(_gameProcess.MainWindowHandle, WM_LBUTTONUP, (IntPtr)0, lParam);
                    break;
                case ClickType.Right:
                    PostMessage(_gameProcess.MainWindowHandle, WM_RBUTTONDOWN, (IntPtr)1, lParam);
                    PostMessage(_gameProcess.MainWindowHandle, WM_RBUTTONUP, (IntPtr)0, lParam);
                    break;
                case ClickType.Middle:
                    PostMessage(_gameProcess.MainWindowHandle, WM_MBUTTONDOWN, (IntPtr)1, lParam);
                    PostMessage(_gameProcess.MainWindowHandle, WM_MBUTTONUP, (IntPtr)0, lParam);
                    break;
                case ClickType.DoubleClick:
                    PostMessage(_gameProcess.MainWindowHandle, WM_LBUTTONDOWN, (IntPtr)1, lParam);
                    PostMessage(_gameProcess.MainWindowHandle, WM_LBUTTONUP, (IntPtr)0, lParam);
                    PostMessage(_gameProcess.MainWindowHandle, WM_LBUTTONDOWN, (IntPtr)1, lParam);
                    PostMessage(_gameProcess.MainWindowHandle, WM_LBUTTONUP, (IntPtr)0, lParam);
                    break;
            }
        }

        private void RedTeamButton_Click(object sender, RoutedEventArgs? e)
        {
            if (_redClickerId.HasValue)
            {
                _clickerService?.Unregister(_redClickerId.Value);
                _redClickerId = null;
                RedTeamButton.Content = "Auto Red Team";
                Status = "Red Team auto-clicker stopped";
            }
            else
            {
                var clickType = (ClickType)ClickTypeComboBox.SelectedItem;
                _redClickerId = _clickerService?.Register(
                    (ct) => ClickTeamButton((int)_settings.RedTeamCoordinates.X, (int)_settings.RedTeamCoordinates.Y, ct),
                    clickType
                );
                RedTeamButton.Content = "Stop Red Team";
                Status = "Red Team auto-clicker started";
            }
        }

        private void BlueTeamButton_Click(object sender, RoutedEventArgs? e)
        {
            if (_blueClickerId.HasValue)
            {
                _clickerService?.Unregister(_blueClickerId.Value);
                _blueClickerId = null;
                BlueTeamButton.Content = "Auto Blue Team";
                Status = "Blue Team auto-clicker stopped";
            }
            else
            {
                var clickType = (ClickType)ClickTypeComboBox.SelectedItem;
                _blueClickerId = _clickerService?.Register(
                    (ct) => ClickTeamButton((int)_settings.BlueTeamCoordinates.X, (int)_settings.BlueTeamCoordinates.Y, ct),
                    clickType
                );
                BlueTeamButton.Content = "Stop Blue Team";
                Status = "Blue Team auto-clicker started";
            }
        }

        #endregion

        private string Status
        {
            set
            {
                Dispatcher.Invoke(() => StatusText.Text = value);
            }
        }

        #region Game Check Timer
        private void InitializeGameCheckTimer()
        {
            _gameCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _gameCheckTimer.Tick += async (_, _) => await GameCheckAsync();
            _gameCheckTimer.Start();
        }


        private async Task GameCheckAsync()
        {
            Process? game = await Task.Run(() => Process.GetProcessesByName("ProTanki").FirstOrDefault());

            if (game != null)
            {
                if (_gameProcess == null || _gameProcess.Id != game.Id)
                {
                    _gameProcess = game;
                    EnableAutomation();
                }
            }
            else
            {
                if (_gameProcess != null)
                {
                    _gameProcess = null;
                    DisableAutomation();
                }
            }
        }
        #endregion

        #region Automation UI
        private void EnableAutomation()
        {
            _powerupUtils?.Initialize();
            PowerupsGroup.Visibility = Visibility.Visible;
            ToggleAllButton.Visibility = Visibility.Visible;
            LaunchButton.Visibility = Visibility.Collapsed;
            Status = "Game is running!";
        }

        private void DisableAutomation()
        {
            _powerupUtils?.StopAll();
            PowerupsPanel.Children.Clear();
            PowerupsGroup.Visibility = Visibility.Collapsed;
            ToggleAllButton.Visibility = Visibility.Collapsed;
            LaunchButton.Visibility = Visibility.Visible;
            Status = "Game not running. Click Launch to start.";
        }
        #endregion

        #region Game Launch / Installation
        private void CheckGameInstallation()
        {
            if (!string.IsNullOrEmpty(_settings.GameExecutablePath) && File.Exists(Path.Combine(_settings.GameExecutablePath, "ProTanki.exe")))
            {
                _gameExecutablePath = Path.Combine(_settings.GameExecutablePath, "ProTanki.exe");
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
            string[] registryPaths =
            [
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            ];

            foreach (string path in registryPaths)
            {
                foreach (RegistryKey baseKey in new[] { Registry.LocalMachine, Registry.CurrentUser })
                {
                    using RegistryKey? key = baseKey.OpenSubKey(path);
                    if (key == null) continue;

                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        using RegistryKey? subKey = key.OpenSubKey(subKeyName);
                        if (subKey == null) continue;

                        string? displayName = subKey.GetValue("DisplayName")?.ToString();
                        if (displayName != null && displayName.Contains("ProTanki Online", StringComparison.OrdinalIgnoreCase))
                        {
                            string? installLocation = subKey.GetValue("InstallLocation")?.ToString();
                            if (!string.IsNullOrEmpty(installLocation))
                            {
                                string executablePath = Path.Combine(installLocation, "ProTanki.exe");
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
            List<string> commonPaths =
            [
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            ];

            foreach (string basePath in commonPaths)
            {
                string directPath = Path.Combine(basePath, "ProTanki.exe");
                if (File.Exists(directPath))
                {
                    return directPath;
                }

                string subDirPath = Path.Combine(basePath, "ProTanki Online", "ProTanki.exe");
                if (File.Exists(subDirPath))
                {
                    return subDirPath;
                }

                subDirPath = Path.Combine(basePath, "ProTanki", "ProTanki.exe");
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
                    Status = "Game executable not found. Please select it.";
                    OpenFileDialog dlg = new()
                    {
                        Filter = "Executable files (*.exe)|*.exe",
                        Title = "Select Pro Tanki Online executable"
                    };

                    if (dlg.ShowDialog() == true)
                    {
                        _gameExecutablePath = dlg.FileName;
                        _settings.GameExecutablePath = Path.GetDirectoryName(dlg.FileName);
                        _settings.Save();
                    }
                    else return;
                }

                _gameProcess = Process.Start(new ProcessStartInfo { FileName = _gameExecutablePath, UseShellExecute = true });
                if (_gameProcess != null)
                {
                    EnableAutomation();
                }
            }
            catch (Exception)
            {
                Status = "Error launching game.";
            }
        }
        #endregion

        #region UI Event Handlers
        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchGame();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new SettingsWindow { Owner = this };
            wnd.ShowDialog();

            _settings = AppSettings.Load();
            if (_clickerService != null)
            {
                _clickerService.ClickSpeed = _settings.ClickSpeed;
            }
            if (_powerupUtils != null) _powerupUtils.Settings = _settings;
            App.ApplyTheme(_settings.Theme);
            RegisterHotkeysFromSettings();

            PauseHotkeysCheckBox.IsChecked = GlobalHotKeyManager.IsPaused;
        }

        private void PauseHotkeysCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GlobalHotKeyManager.IsPaused = true;
        }

        private void PauseHotkeysCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalHotKeyManager.IsPaused = false;
        }
        #endregion

        #region Helpers

        protected override void OnClosed(EventArgs e)
        {
            _gameCheckTimer?.Stop();
            _powerupUtils?.StopAll();
            _clickerService?.Dispose();
            GlobalHotKeyManager.Shutdown();
            _notifyIcon?.Dispose();

            base.OnClosed(e);
        }
        #endregion

        #region Tray Icon

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Visible = false, // Initially hidden
                Text = "Automatization"
            };

            try
            {
                // Make sure you have an 'icon.ico' file in the 'Settings' folder of your project
                _notifyIcon.Icon = new Icon("Settings/icon.ico");
            }
            catch (Exception)
            {
            }

            _notifyIcon.DoubleClick += (s, args) => ShowWindow();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Toggle All", null, (s, e) => _powerupUtils?.ToggleAll());
            contextMenu.Items.Add("Exit", null, (s, e) => Close());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _notifyIcon != null)
            {
                Hide();
                _notifyIcon.Visible = true;
            }
            base.OnStateChanged(e);
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            if (_notifyIcon != null) _notifyIcon.Visible = false;
            Activate();
        }
        #endregion
    }


}
