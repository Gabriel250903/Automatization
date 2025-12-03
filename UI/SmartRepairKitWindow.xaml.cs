using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace Automatization.UI
{
    public partial class SmartRepairKitWindow : FluentWindow
    {
        private readonly SmartRepairKitService _service;
        private bool _isMonitoring = false;

        public SmartRepairKitWindow()
        {
            InitializeComponent();
            _service = new SmartRepairKitService();

            _service.OnStateUpdated += Service_OnStateUpdated;

            LoadSettings();
            PopulateKeys();
            UpdateConfig();
        }

        public void ToggleMonitoring()
        {
            BtnToggle_Click(null, null);
        }

        public void SaveDebugSnapshot()
        {
            BtnDebug_Click(null, null);
        }

        private void LoadSettings()
        {
            AppSettings settings = AppSettings.Load();

            SldThreshold.Value = settings.SmartRepairThreshold;
            SldCooldown.Value = settings.SmartRepairCooldown;
            CmbKey.SelectedItem = settings.SmartRepairKey;

            _service.TargetFps = settings.SmartRepairFps;

            if (settings.SmartRepairToggleHotKey != null)
            {
                BtnToggle.Content = $"Start Monitoring ({settings.SmartRepairToggleHotKey})";
            }

            if (settings.UseCustomHealthColors &&
                !string.IsNullOrEmpty(settings.CustomHealthBrightColor) &&
                !string.IsNullOrEmpty(settings.CustomHealthDarkColor))
            {
                try
                {
                    Color c1 = ColorTranslator.FromHtml(settings.CustomHealthBrightColor);
                    Color c2 = ColorTranslator.FromHtml(settings.CustomHealthDarkColor);

                    _service.UpdateColors(c1, c2);

                    _pickedFullColor = System.Windows.Media.Color.FromRgb(c1.R, c1.G, c1.B);
                    _pickedEmptyColor = System.Windows.Media.Color.FromRgb(c2.R, c2.G, c2.B);

                    RectFullColor.Fill = new System.Windows.Media.SolidColorBrush(_pickedFullColor);
                    RectEmptyColor.Fill = new System.Windows.Media.SolidColorBrush(_pickedEmptyColor);

                    TxtStatus.Text = "Custom colors loaded.";
                }
                catch (Exception ex)
                {
                    LogService.LogError("Failed to parse custom colors from settings.", ex);
                }
            }
        }

        private void SaveSettings()
        {
            AppSettings settings = AppSettings.Load();

            settings.SmartRepairThreshold = (int)SldThreshold.Value;
            settings.SmartRepairCooldown = (int)SldCooldown.Value;
            if (CmbKey.SelectedItem is Key k)
            {
                settings.SmartRepairKey = k;
            }

            settings.Save();
        }

        // private async Task HotkeyLoop() ... Removed

        private void PopulateKeys()
        {
            List<Key> keys =
            [
                Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9
            ];

            CmbKey.ItemsSource = keys;
            CmbKey.SelectedItem = Key.D1;
        }

        private void BtnToggle_Click(object? sender, RoutedEventArgs? e)
        {
            if (!_isMonitoring)
            {
                _isMonitoring = true;
                _service.Start();
                BtnToggle.Content = "Stop Monitoring";
                BtnToggle.Appearance = ControlAppearance.Danger;
                BtnToggle.Icon = new SymbolIcon(SymbolRegular.Stop24);
                TxtStatus.Text = "Searching for Health Bar...";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                _isMonitoring = false;
                _service.Stop();
                BtnToggle.Content = "Start Monitoring";
                BtnToggle.Appearance = ControlAppearance.Primary;
                BtnToggle.Icon = new SymbolIcon(SymbolRegular.Play24);
                TxtStatus.Text = "Stopped";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
                TxtHealth.Text = "--";
            }
        }

        private void BtnDebug_Click(object? sender, RoutedEventArgs? e)
        {
            AdminPasswordDialog passwordDialog = new() { Owner = this };
            if (passwordDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "debug_capture.png");

                ScreenCaptureService tempCapture = new();
                using (Bitmap bmp = ScreenCaptureService.CaptureScreen())
                {
                    HealthBarStruct state = _service.Detector.Detect(bmp);

                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        if (state.IsFound)
                        {
                            using (Pen pen = new(Color.Red, 3))
                            {
                                g.DrawRectangle(pen, state.Bounds);
                            }

                            using Font font = new("Arial", 20);
                            using SolidBrush brush = new(Color.Yellow);

                            g.DrawString($"{state.HealthPercentage:F1}%", font, brush, state.Bounds.X, state.Bounds.Y - 30);
                        }
                        else
                        {
                            using Font font = new("Arial", 40);
                            using SolidBrush brush = new(Color.Red);
                            g.DrawString("No Health Bar Found", font, brush, 100, 100);
                        }
                    }

                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }

                _ = MessageBox.Show($"Screenshot saved to:\n{path}\n\nLook for a RED BOX. If the box only covers the 'Full' part of your bar, the tool is learning the width. Go to 100% health once to fix it.", "Debug Capture");
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Failed to save screenshot: {ex.Message}", "Error");
            }
        }

        private void Service_OnStateUpdated(HealthBarStruct state)
        {
            _ = Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_isMonitoring)
                {
                    return;
                }

                if (state.IsFound)
                {
                    TxtStatus.Text = $"Tracking health bar...";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Green;

                    TxtHealth.Text = $"{state.HealthPercentage:F0}%";

                    TxtHealth.Foreground = state.HealthPercentage < SldThreshold.Value ? System.Windows.Media.Brushes.Red : (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["Foreground"];
                }
                else
                {
                    TxtStatus.Text = "Searching...";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                }
            }));
        }

        private System.Windows.Media.Color _pickedFullColor = System.Windows.Media.Colors.Gray;
        private System.Windows.Media.Color _pickedEmptyColor = System.Windows.Media.Colors.Gray;

        private async void BtnPickFull_Click(object sender, RoutedEventArgs e)
        {
            await PickColorAsync(true);
        }

        private async void BtnPickEmpty_Click(object sender, RoutedEventArgs e)
        {
            await PickColorAsync(false);
        }

        private async Task PickColorAsync(bool isFull)
        {
            for (int i = 5; i > 0; i--)
            {
                TxtStatus.Text = $"Picking in {i}... Hover over {(isFull ? "FULL" : "EMPTY")} bar!";
                await Task.Delay(1000);
            }

            Color capturedColor;

            using (Bitmap bmp = ScreenCaptureService.CaptureScreen())
            {
                System.Drawing.Point point = System.Windows.Forms.Cursor.Position;
                capturedColor = point.X >= 0 && point.X < bmp.Width && point.Y >= 0 && point.Y < bmp.Height ? bmp.GetPixel(point.X, point.Y) : Color.Black;
            }

            System.Windows.Media.Color mediaColor = System.Windows.Media.Color.FromRgb(capturedColor.R, capturedColor.G, capturedColor.B);
            if (isFull)
            {
                _pickedFullColor = mediaColor;
                RectFullColor.Fill = new System.Windows.Media.SolidColorBrush(mediaColor);
                TxtStatus.Text = $"Picked Full: {capturedColor}";
            }
            else
            {
                _pickedEmptyColor = mediaColor;
                RectEmptyColor.Fill = new System.Windows.Media.SolidColorBrush(mediaColor);
                TxtStatus.Text = $"Picked Empty: {capturedColor}";
            }
        }

        private void BtnApplyColors_Click(object sender, RoutedEventArgs e)
        {
            if (_pickedFullColor == System.Windows.Media.Colors.Gray || _pickedEmptyColor == System.Windows.Media.Colors.Gray)
            {
                _ = MessageBox.Show("Please pick both Full and Empty colors first.", "Calibration");
                return;
            }

            Color c1 = Color.FromArgb(_pickedFullColor.R, _pickedFullColor.G, _pickedFullColor.B);
            Color c2 = Color.FromArgb(_pickedEmptyColor.R, _pickedEmptyColor.G, _pickedEmptyColor.B);

            _service.UpdateColors(c1, c2);

            AppSettings settings = AppSettings.Load();
            settings.UseCustomHealthColors = true;
            settings.CustomHealthBrightColor = ColorTranslator.ToHtml(c1);
            settings.CustomHealthDarkColor = ColorTranslator.ToHtml(c2);
            settings.Save();

            TxtStatus.Text = "Custom colors applied & saved!";
        }

        private void SldThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateConfig();
        }

        private void SldCooldown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateConfig();
        }

        private void CmbKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateConfig();
        }

        private void BtnTestInput_Click(object sender, RoutedEventArgs e)
        {
            _ = MessageBox.Show("In 5 seconds, the tool will press the selected key.\n\nOpen the in-game chat to verify if it types.", "Input Test");
            _ = Task.Delay(5000).ContinueWith(_ =>
            {
                _service.ForceTrigger();
            });
        }

        private void UpdateConfig()
        {
            if (_service == null)
            {
                return;
            }

            _service.HealthThreshold = (int)SldThreshold.Value;
            _service.CooldownMs = (int)SldCooldown.Value;
            if (CmbKey.SelectedItem is Key k)
            {
                _service.ActivationKey = k;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
            _service.Stop();
            _service.Dispose();
        }
    }
}