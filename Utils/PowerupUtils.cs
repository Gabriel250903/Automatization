using Automatization.Settings;
﻿using Automatization.Types;
﻿using System.Runtime.InteropServices;
﻿using System.Windows;
﻿using System.Windows.Controls;
﻿using System.Windows.Input;
﻿using System.Windows.Threading;
﻿using Button = System.Windows.Controls.Button;
﻿using Orientation = System.Windows.Controls.Orientation;
﻿using Panel = System.Windows.Controls.Panel;
﻿using Automatization.Services;
﻿
﻿namespace Automatization.Utils
﻿{
﻿    public class PowerupUtils(AppSettings settings, Panel panel, Button toggleAllButton)
﻿    {
﻿        public AppSettings Settings { get; set; } = settings;
﻿
﻿        private Panel _panel = panel;
﻿        private Button _toggleAllButton = toggleAllButton;
﻿
﻿        private Dictionary<PowerupType, DispatcherTimer> _timers = [];
﻿        private Dictionary<PowerupType, bool> _states = [];
﻿
﻿        [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
﻿        private const uint KEYEVENTF_KEYUP = 0x0002;
﻿
﻿        public void Initialize()
﻿        {
﻿            LogService.LogInfo("Initializing PowerupUtils.");
﻿            _panel.ClearChildren();
﻿            _panel.ParentAsGroupBox()?.Show();
﻿
﻿            bool settingsChanged = false;
﻿
﻿            foreach (PowerupType powerup in Enum.GetValues<PowerupType>())
﻿            {
﻿                _states[powerup] = false;
﻿
﻿                double intervalMs;
﻿
﻿                if (!Settings.PowerupDelays.TryGetValue(powerup, out double value))
﻿                {
﻿                    intervalMs = 1000;
﻿                    Settings.PowerupDelays[powerup] = intervalMs;
﻿                    settingsChanged = true;
﻿                    LogService.LogInfo($"Default delay set for {powerup}: {intervalMs}ms.");
﻿                }
﻿                else
﻿                {
﻿                    intervalMs = value;
﻿                }
﻿
﻿                DispatcherTimer timer = new()
﻿                {
﻿                    Interval = TimeSpan.FromMilliseconds(intervalMs)
﻿                };
﻿                timer.Tick += (_, _) => UsePowerup(powerup);
﻿                _timers[powerup] = timer;
﻿
﻿                CreatePowerupControl(powerup, intervalMs);
﻿            }
﻿
﻿            if (settingsChanged)
﻿            {
﻿                Settings.Save();
﻿                LogService.LogInfo("Powerup settings saved due to changes.");
﻿            }
﻿            LogService.LogInfo("PowerupUtils initialized.");
﻿        }
﻿
﻿        private void CreatePowerupControl(PowerupType powerup, double savedDelay)
﻿        {
﻿            StackPanel stack = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
﻿
﻿            Button btn = new()
﻿            {
﻿                Content = $"Start {powerup}",
﻿                Tag = powerup,
﻿                Width = 120,
﻿                Height = 30,
﻿                Margin = new Thickness(0, 0, 10, 0)
﻿            };
﻿            btn.Click += PowerupButton_Click;
﻿
﻿            Slider slider = new()
﻿            {
﻿                Width = 100,
﻿                Minimum = 1,
﻿                Maximum = 5000,
﻿                Value = savedDelay,
﻿                Tag = powerup,
﻿                TickFrequency = 10,
﻿                IsSnapToTickEnabled = true
﻿            };
﻿            slider.ValueChanged += PowerupDelay_ValueChanged;
﻿
﻿            TextBlock delayText = new() { Text = $"{savedDelay}ms", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
﻿
﻿            _ = stack.Children.Add(btn);
﻿            _ = stack.Children.Add(slider);
﻿            _ = stack.Children.Add(delayText);
﻿
﻿            _ = _panel.Children.Add(stack);
﻿            LogService.LogInfo($"Created control for powerup: {powerup} with delay {savedDelay}ms.");
﻿        }
﻿
﻿        private void PowerupButton_Click(object sender, RoutedEventArgs e)
﻿        {
﻿            if (sender is not Button btn || btn.Tag is not PowerupType powerup)
﻿            {
﻿                LogService.LogWarning("PowerupButton_Click: Invalid sender or tag.");
﻿                return;
﻿            }
﻿
﻿            _states[powerup] = !_states[powerup];
﻿            UpdateButton(powerup, btn);
﻿            LogService.LogInfo($"Powerup {powerup} state toggled to {_states[powerup]}.");
﻿        }
﻿
﻿        private void PowerupDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
﻿        {
﻿            if (sender is not Slider slider || slider.Tag is not PowerupType powerup)
﻿            {
﻿                LogService.LogWarning("PowerupDelay_ValueChanged: Invalid sender or tag.");
﻿                return;
﻿            }
﻿
﻿            if (_timers.TryGetValue(powerup, out DispatcherTimer? timer))
﻿            {
﻿                timer.Interval = TimeSpan.FromMilliseconds(e.NewValue);
﻿                Settings.PowerupDelays[powerup] = e.NewValue;
﻿                Settings.Save();
﻿                LogService.LogInfo($"Powerup {powerup} delay changed to {e.NewValue:0}ms.");
﻿            }
﻿
﻿            if (slider.Parent is StackPanel panel && panel.Children.Count > 2 && panel.Children[2] is TextBlock txt)
﻿            {
﻿                txt.Text = $"{e.NewValue:0}ms";
﻿            }
﻿        }
﻿
﻿        private void UpdateButton(PowerupType powerup, Button btn)
﻿        {
﻿            if (_states[powerup])
﻿            {
﻿                btn.Content = $"Stop {powerup}";
﻿                _timers[powerup].Start();
﻿                LogService.LogInfo($"Powerup {powerup} started.");
﻿            }
﻿            else
﻿            {
﻿                btn.Content = $"Start {powerup}";
﻿                _timers[powerup].Stop();
﻿                LogService.LogInfo($"Powerup {powerup} stopped.");
﻿            }
﻿        }
﻿
﻿        public bool ToggleAll()
﻿        {
﻿            LogService.LogInfo("Toggling all powerups.");
﻿            bool anyEnabled = _states.Values.Any(v => v);
﻿            bool newState = !anyEnabled;
﻿
﻿            foreach (PowerupType powerup in _states.Keys.ToList())
﻿            {
﻿                _states[powerup] = newState;
﻿
﻿                foreach (StackPanel panel in _panel.Children.OfType<StackPanel>())
﻿                {
﻿                    if (panel.Children[0] is Button btn && btn.Tag is PowerupType p && p == powerup)
﻿                    {
﻿                        UpdateButton(powerup, btn);
﻿                    }
﻿                }
﻿            }
﻿            LogService.LogInfo($"All powerups set to state: {newState}.");
﻿            return newState;
﻿        }
﻿
﻿        public void StopAll()
﻿        {
﻿            LogService.LogInfo("Stopping all powerups.");
﻿            foreach (DispatcherTimer timer in _timers.Values)
﻿            {
﻿                timer.Stop();
﻿            }
﻿
﻿            foreach (PowerupType key in _states.Keys.ToList())
﻿            {
﻿                _states[key] = false;
﻿            }
﻿            LogService.LogInfo("All powerups stopped.");
﻿        }
﻿
﻿        private void UsePowerup(PowerupType powerup)
﻿        {
﻿            try
﻿            {
﻿                if (!Settings.PowerupKeys.TryGetValue(powerup, out Key key))
﻿                {
﻿                    LogService.LogWarning($"Attempted to use powerup {powerup}, but no key is assigned.");
﻿                    return;
﻿                }
﻿
﻿                byte vk = (byte)KeyInterop.VirtualKeyFromKey(key);
﻿                keybd_event(vk, 0, 0, 0);
﻿                keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
﻿                LogService.LogInfo($"Used powerup {powerup} with key {key}.");
﻿            }
﻿            catch (Exception ex)
﻿            {
﻿                LogService.LogError($"Error using powerup {powerup}.", ex);
﻿            }
﻿        }
﻿    }
﻿}