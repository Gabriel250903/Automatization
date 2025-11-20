using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using Automatization.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace Automatization.Utils
{
    public class PowerupUtils
    {
        public AppSettings Settings { get; set; }
        public Process? GameProcess { get; set; }

        private ObservableCollection<PowerupViewModel> _powerups = [];
        public ReadOnlyObservableCollection<PowerupViewModel> Powerups { get; }

        private Dictionary<PowerupType, DispatcherTimer> _timers = [];
        private Dictionary<PowerupType, bool> _activeStates = [];
        private Dictionary<PowerupType, bool> _pausedStates = [];

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;

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

        public void UpdateSettings(AppSettings newSettings)
        {
            LogService.LogInfo("Updating PowerupUtils settings.");

            Settings = newSettings;

            foreach (PowerupViewModel viewModel in _powerups)
            {
                if (Settings.PowerupDelays.TryGetValue(viewModel.PowerupType, out double newDelay))
                {
                    viewModel.Delay = newDelay;
                    if (_timers.TryGetValue(viewModel.PowerupType, out DispatcherTimer? timer))
                    {
                        timer.Interval = TimeSpan.FromMilliseconds(newDelay);
                    }
                }
            }

            LogService.LogInfo("PowerupUtils settings updated.");
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

        public void PauseAll()
        {
            LogService.LogInfo("Pausing all powerups.");

            _pausedStates = new Dictionary<PowerupType, bool>(_activeStates);

            foreach (PowerupType powerup in _timers.Keys)
            {
                if (_activeStates.TryGetValue(powerup, out bool isActive) && isActive)
                {
                    StopPowerupTimer(powerup);
                }
            }

            LogService.LogInfo("All powerups paused.");
        }

        public void ResumeAll()
        {
            LogService.LogInfo("Resuming all powerups.");

            foreach (PowerupType powerup in _timers.Keys)
            {
                if (_pausedStates.TryGetValue(powerup, out bool wasActive) && wasActive)
                {
                    StartPowerupTimer(powerup);
                }
            }

            _pausedStates.Clear();

            LogService.LogInfo("All powerups resumed.");
        }

        private void UsePowerup(PowerupType powerup)
        {
            if (!WindowUtils.IsGameWindowInForeground(GameProcess))
            {
                LogService.LogWarning("Game window is not in the foreground. Powerup activation skipped.");
                return;
            }

            if (GameProcess == null || GameProcess.MainWindowHandle == IntPtr.Zero)
            {
                LogService.LogWarning("Game process not found, cannot send powerup key press.");
                return;
            }

            try
            {
                if (!Settings.PowerupKeys.TryGetValue(powerup, out Key key))
                {
                    LogService.LogWarning($"Attempted to use powerup {powerup}, but no key is assigned.");
                    return;
                }

                IntPtr vk = KeyInterop.VirtualKeyFromKey(key);
                _ = PostMessage(GameProcess.MainWindowHandle, WM_KEYDOWN, vk, IntPtr.Zero);
                _ = PostMessage(GameProcess.MainWindowHandle, WM_KEYUP, vk, IntPtr.Zero);

                LogService.LogInfo($"Sent powerup {powerup} with key {key}.");
            }
            catch (Exception ex)
            {
                LogService.LogError($"Error using powerup {powerup}.", ex);
            }
        }
    }
}
