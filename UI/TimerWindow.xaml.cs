using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Automatization.Services;
using Automatization.Settings;
using Automatization.Utils;
using Brushes = System.Windows.Media.Brushes;

namespace Automatization.UI
{
    public partial class TimerWindow : Window
    {
        private DispatcherTimer _timer;
        private DispatcherTimer _hideTimer;
        private CancellationTokenSource? _bgLoopCts;
        private int _seconds;
        private const int MaxSeconds = 40;
        private string _gameProcessName;
        private bool _isPaused = false;
        private bool _isGameRunning = false;

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

            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _hideTimer.Tick += HideTimer_Tick;

            StartBackgroundGameCheck();

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
            _ = SetWindowLongPtr(
                helper.Handle,
                GWL_EXSTYLE,
                new IntPtr(currentStyle.ToInt64() | WS_EX_NOACTIVATE)
            );
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

        private void StartBackgroundGameCheck()
        {
            _bgLoopCts = new CancellationTokenSource();
            CancellationToken token = _bgLoopCts.Token;

            Task.Run(
                    async () =>
                    {
                        using PeriodicTimer periodicTimer = new(TimeSpan.FromMilliseconds(500));
                        while (!token.IsCancellationRequested)
                        {
                            try
                            {
                                if (
                                    !await periodicTimer
                                        .WaitForNextTickAsync(token)
                                        .ConfigureAwait(false)
                                )
                                {
                                    break;
                                }

                                Process? currentGame = WindowUtils.GetFirstProcessByName(
                                    _gameProcessName
                                );
                                bool isRunning = currentGame != null;
                                bool isForeground = false;

                                if (currentGame != null)
                                {
                                    isForeground = WindowUtils.IsGameWindowInForeground(
                                        currentGame
                                    );
                                    currentGame.Dispose();
                                }

                                if (!token.IsCancellationRequested)
                                {
                                    _ = Dispatcher.InvokeAsync(
                                        () =>
                                        {
                                            OnGameStatusUpdated(isRunning, isForeground);
                                        },
                                        DispatcherPriority.Background
                                    );
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                LogService.LogWarning(
                                    $"Background game check error in TimerWindow: {ex.Message}"
                                );
                            }
                        }
                    },
                    token
                )
                .SafeFireAndForget("TimerWindow.BackgroundGameCheck");
        }

        private void OnGameStatusUpdated(bool isRunning, bool isForeground)
        {
            _isGameRunning = isRunning;
            if (!isRunning)
            {
                HideTimerWindow();
                return;
            }

            if (isForeground)
            {
                ShowTimerWindow();
            }
            else
            {
                DebounceHideTimerWindow();
            }
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
            _bgLoopCts?.Cancel();
            _bgLoopCts?.Dispose();
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
