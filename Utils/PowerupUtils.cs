using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using Automatization.ViewModels;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace Automatization.Utils
{
    public class PowerupUtils
    {
        public AppSettings Settings { get; set; }

        private ObservableCollection<PowerupViewModel> _powerups = [];
        public ReadOnlyObservableCollection<PowerupViewModel> Powerups { get; }

        private Dictionary<PowerupType, DispatcherTimer> _timers = [];
        private Dictionary<PowerupType, bool> _activeStates = [];

        [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public PowerupUtils(AppSettings settings)
        {
            Settings = settings;
            Powerups = new ReadOnlyObservableCollection<PowerupViewModel>(_powerups);
        }

        public void Initialize()
        {
            LogService.LogInfo("Initializing PowerupUtils.");
            _powerups.Clear();
            _timers.Clear();
            _activeStates.Clear();

            bool settingsChanged = false;

            foreach (PowerupType powerup in Enum.GetValues<PowerupType>())
            {
                double intervalMs;

                if (!Settings.PowerupDelays.TryGetValue(powerup, out double value))
                {
                    intervalMs = 1000;
                    Settings.PowerupDelays[powerup] = intervalMs;
                    settingsChanged = true;
                    LogService.LogInfo($"Default delay set for {powerup}: {intervalMs}ms.");
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
                _activeStates[powerup] = false;

                PowerupViewModel viewModel = new(powerup, intervalMs, Settings, UsePowerup, SaveDelay);
                viewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(PowerupViewModel.IsActive))
                    {
                        if (sender is PowerupViewModel vm)
                        {
                            if (vm.IsActive)
                            {
                                StartPowerupTimer(vm.PowerupType);
                            }
                            else
                            {
                                StopPowerupTimer(vm.PowerupType);
                            }
                        }
                    }
                    else if (e.PropertyName == nameof(PowerupViewModel.Delay))
                    {
                        if (sender is PowerupViewModel vm && _timers.TryGetValue(vm.PowerupType, out DispatcherTimer? t))
                        {
                            t.Interval = TimeSpan.FromMilliseconds(vm.Delay);
                        }
                    }
                };
                _powerups.Add(viewModel);
            }

            if (settingsChanged)
            {
                Settings.Save();
                LogService.LogInfo("Powerup settings saved due to changes.");
            }
            LogService.LogInfo("PowerupUtils initialized.");
        }

        private void SaveDelay(PowerupType powerup, double delay)
        {
            Settings.PowerupDelays[powerup] = delay;
            Settings.Save();
            LogService.LogInfo($"Powerup {powerup} delay saved to {delay}ms.");
        }

        private void StartPowerupTimer(PowerupType powerup)
        {
            if (_timers.TryGetValue(powerup, out DispatcherTimer? timer))
            {
                timer.Start();
                _activeStates[powerup] = true;
                LogService.LogInfo($"Powerup {powerup} timer started.");
            }
        }

        private void StopPowerupTimer(PowerupType powerup)
        {
            if (_timers.TryGetValue(powerup, out DispatcherTimer? timer))
            {
                timer.Stop();
                _activeStates[powerup] = false;
                LogService.LogInfo($"Powerup {powerup} timer stopped.");
            }
        }

        public bool ToggleAll()
        {
            LogService.LogInfo("Toggling all powerups.");
            bool anyActive = _powerups.Any(vm => vm.IsActive);
            bool newState = !anyActive;

            foreach (PowerupViewModel viewModel in _powerups)
            {
                viewModel.IsActive = newState;
            }
            LogService.LogInfo($"All powerups set to state: {newState}.");
            return newState;
        }

        public void StopAll()
        {
            LogService.LogInfo("Stopping all powerups.");
            foreach (PowerupViewModel viewModel in _powerups)
            {
                viewModel.IsActive = false;
            }
            LogService.LogInfo("All powerups stopped.");
        }

        private void UsePowerup(PowerupType powerup)
        {
            try
            {
                if (!Settings.PowerupKeys.TryGetValue(powerup, out Key key))
                {
                    LogService.LogWarning($"Attempted to use powerup {powerup}, but no key is assigned.");
                    return;
                }

                byte vk = (byte)KeyInterop.VirtualKeyFromKey(key);
                keybd_event(vk, 0, 0, 0);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
                LogService.LogInfo($"Used powerup {powerup} with key {key}.");
            }
            catch (Exception ex)
            {
                LogService.LogError($"Error using powerup {powerup}.", ex);
            }
        }
    }
}
