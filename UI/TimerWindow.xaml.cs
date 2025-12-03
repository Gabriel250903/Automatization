using Automatization.Settings;
using Automatization.Utils;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;

namespace Automatization.UI
{
    public partial class TimerWindow : Window
    {
        private DispatcherTimer _timer;
        private DispatcherTimer _gameCheckTimer;
        private DispatcherTimer _hideTimer;
        private int _seconds;
        private const int MaxSeconds = 40;
        private Process? _gameProcess;
        private string _gameProcessName;
        private bool _isPaused = false;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public TimerWindow(bool startTimer = false)
        {
            InitializeComponent();

            _seconds = MaxSeconds;
            UpdateTimerDisplay();

            AppSettings settings = AppSettings.Load();
            _gameProcessName = settings.GameProcessName;

            if (settings.IsTimerWindowTransparent)
            {
                BackgroundBorder.Background = Brushes.Transparent;
                BackgroundBorder.BorderThickness = new Thickness(0);
            }

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            _timer.Tick += Timer_Tick;

            _gameCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _gameCheckTimer.Tick += GameCheckTimer_Tick;
            _gameCheckTimer.Start();

            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _hideTimer.Tick += HideTimer_Tick;

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = 100;
            Top = 100;

            if (startTimer)
            {
                Start();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowInteropHelper helper = new(this);
            IntPtr currentStyle = GetWindowLongPtr(helper.Handle, GWL_EXSTYLE);
            _ = SetWindowLongPtr(helper.Handle, GWL_EXSTYLE, new IntPtr(currentStyle.ToInt64() | WS_EX_NOACTIVATE));
        }

        public void Start()
        {
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_isPaused)
            {
                _seconds--;
                UpdateTimerDisplay();

                if (_seconds <= 0)
                {
                    _timer.Stop();
                    Close();
                }
            }
        }

        private void UpdateTimerDisplay()
        {
            TimerLabel.Text = _seconds.ToString();
            double progress = (double)_seconds / MaxSeconds * 100;
            TimerProgressRing.Progress = progress;

            if (_seconds <= 5)
            {
                TimerLabel.Foreground = Brushes.Red;
                TimerProgressRing.Foreground = Brushes.Red;
            }
        }

        private void GameCheckTimer_Tick(object? sender, EventArgs e)
        {
            Process? currentGame = Process.GetProcessesByName(_gameProcessName).FirstOrDefault();

            if (currentGame == null)
            {
                HandleGameProcessLost();
                return;
            }
            HandleGameProcessFound(currentGame);
        }

        private void HandleGameProcessFound(Process game)
        {
            if (_gameProcess == null || _gameProcess.Id != game.Id)
            {
                _gameProcess = game;
            }

            if (WindowUtils.IsGameWindowInForeground(_gameProcess))
            {
                ShowTimerWindow();
            }
            else
            {
                DebounceHideTimerWindow();
            }
        }

        private void HandleGameProcessLost()
        {
            _gameProcess = null;
            HideTimerWindow();
        }

        private void ShowTimerWindow()
        {
            _hideTimer.Stop();
            if (Visibility == Visibility.Visible)
            {
                return;
            }

            Visibility = Visibility.Visible;
        }

        private void HideTimer_Tick(object? sender, EventArgs e)
        {
            _hideTimer.Stop();
            HideTimerWindow();
        }

        private void DebounceHideTimerWindow()
        {
            if (Visibility == Visibility.Hidden || _hideTimer.IsEnabled)
            {
                return;
            }

            _hideTimer.Start();
        }

        private void HideTimerWindow()
        {
            if (Visibility == Visibility.Hidden)
            {
                return;
            }

            Visibility = Visibility.Hidden;
        }

        protected override void OnClosed(EventArgs e)
        {
            _gameCheckTimer.Stop();
            _timer.Stop();
            _hideTimer.Stop();
            base.OnClosed(e);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double scale = e.Delta > 0 ? 1.1 : 0.9;

            double newWidth = Width * scale;
            double newHeight = Height * scale;

            if (newWidth is > 50 and < 500)
            {
                Width = newWidth;
                Height = newHeight;
            }

            e.Handled = true;
        }

        private void PauseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = !_isPaused;
            PauseMenuItem.Header = _isPaused ? "Resume" : "Pause";
        }

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}