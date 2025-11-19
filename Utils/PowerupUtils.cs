﻿using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Automatization.Settings;
using Automatization.Types;
using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;
using Panel = System.Windows.Controls.Panel;

namespace Automatization.Utils
{
    public class PowerupUtils(AppSettings settings, Panel panel, Button toggleAllButton)
    {
        public AppSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }
        private AppSettings _settings = settings;
        private Panel _panel = panel;
        private Button _toggleAllButton = toggleAllButton;

        private Dictionary<PowerupType, DispatcherTimer> _timers = [];
        private Dictionary<PowerupType, bool> _states = [];

        [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public void Initialize()
        {
            _panel.ClearChildren();
            _panel.ParentAsGroupBox()?.Show();

            bool settingsChanged = false;

            foreach (PowerupType powerup in Enum.GetValues<PowerupType>())
            {
                _states[powerup] = false;

                double intervalMs;

                if (!_settings.PowerupDelays.TryGetValue(powerup, out double value))
                {
                    intervalMs = 1000;
                    _settings.PowerupDelays[powerup] = intervalMs;
                    settingsChanged = true;
                }
                else
                {
                    intervalMs = value;
                }

                DispatcherTimer timer = new()
                {
                    Interval = TimeSpan.FromMilliseconds(intervalMs)
                };
                timer.Tick += (_, _) => UsePowerup(powerup);
                _timers[powerup] = timer;

                CreatePowerupControl(powerup, intervalMs);
            }

            if (settingsChanged)
            {
                _settings.Save();
            }
        }

        private void CreatePowerupControl(PowerupType powerup, double savedDelay)
        {
            StackPanel stack = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

            Button btn = new()
            {
                Content = $"Start {powerup}",
                Tag = powerup,
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            btn.Click += PowerupButton_Click;

            Slider slider = new()
            {
                Width = 100,
                Minimum = 1,
                Maximum = 5000,
                Value = savedDelay,
                Tag = powerup,
                TickFrequency = 10,
                IsSnapToTickEnabled = true
            };
            slider.ValueChanged += PowerupDelay_ValueChanged;

            TextBlock delayText = new() { Text = $"{savedDelay}ms", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };

            _ = stack.Children.Add(btn);
            _ = stack.Children.Add(slider);
            _ = stack.Children.Add(delayText);

            _ = _panel.Children.Add(stack);
        }

        private void PowerupButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not PowerupType powerup)
            {
                return;
            }

            _states[powerup] = !_states[powerup];
            UpdateButton(powerup, btn);
        }

        private void PowerupDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is not Slider slider || slider.Tag is not PowerupType powerup)
            {
                return;
            }

            if (_timers.TryGetValue(powerup, out DispatcherTimer? timer))
            {
                timer.Interval = TimeSpan.FromMilliseconds(e.NewValue);
                _settings.PowerupDelays[powerup] = e.NewValue;
                _settings.Save();
            }

            if (slider.Parent is StackPanel panel && panel.Children.Count > 2 && panel.Children[2] is TextBlock txt)
            {
                txt.Text = $"{e.NewValue:0}ms";
            }
        }

        private void UpdateButton(PowerupType powerup, Button btn)
        {
            if (_states[powerup])
            {
                btn.Content = $"Stop {powerup}";
                _timers[powerup].Start();
            }
            else
            {
                btn.Content = $"Start {powerup}";
                _timers[powerup].Stop();
            }
        }

        public bool ToggleAll()
        {
            bool anyEnabled = _states.Values.Any(v => v);
            bool newState = !anyEnabled;

            foreach (PowerupType powerup in _states.Keys.ToList())
            {
                _states[powerup] = newState;

                foreach (StackPanel panel in _panel.Children.OfType<StackPanel>())
                {
                    if (panel.Children[0] is Button btn && btn.Tag is PowerupType p && p == powerup)
                    {
                        UpdateButton(powerup, btn);
                    }
                }
            }
            return newState;
        }

        public void StopAll()
        {
            foreach (DispatcherTimer timer in _timers.Values)
            {
                timer.Stop();
            }

            foreach (PowerupType key in _states.Keys.ToList())
            {
                _states[key] = false;
            }
        }

        private void UsePowerup(PowerupType powerup)
        {
            try
            {
                if (!_settings.PowerupKeys.TryGetValue(powerup, out Key key))
                {
                    return;
                }

                byte vk = (byte)KeyInterop.VirtualKeyFromKey(key);
                keybd_event(vk, 0, 0, 0);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}