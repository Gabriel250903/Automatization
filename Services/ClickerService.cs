using Automatization.Settings;
using Automatization.Types;
using System.Diagnostics;
using Timer = System.Threading.Timer;

namespace Automatization.Services
{
    public class ClickerService : IDisposable
    {
        private readonly Dictionary<Guid, Timer> _timers = [];
        private readonly AppSettings _settings;

        private Process? _cachedProcess;
        private DateTime _lastProcessCheck = DateTime.MinValue;
        private readonly object _processLock = new();

        public ClickerService(AppSettings settings)
        {
            _settings = settings;
            LogService.LogInfo("Clicker service created.");
        }

        public double ClickSpeed
        {
            get => _settings.ClickSpeed;
            set
            {
                _settings.ClickSpeed = value;
                UpdateTimersInterval();
            }
        }

        private Process? GetGameProcess(string gameProcessName)
        {
            lock (_processLock)
            {
                if (_cachedProcess == null || (DateTime.Now - _lastProcessCheck).TotalSeconds >= 2.0)
                {
                    if (_cachedProcess != null)
                    {
                        try
                        {
                            _cachedProcess.Refresh();
                            if (_cachedProcess.HasExited)
                            {
                                _cachedProcess = null;
                            }
                        }
                        catch
                        {
                            _cachedProcess = null;
                        }
                    }

                    _cachedProcess ??= Process.GetProcessesByName(gameProcessName).FirstOrDefault();

                    _lastProcessCheck = DateTime.Now;
                }

                return _cachedProcess;
            }
        }

        public Guid Register(Action<IntPtr, ClickType> clickAction, ClickType clickType, string gameProcessName)
        {
            Guid id = Guid.NewGuid();

            System.Threading.Timer timer = new(
                _ =>
                {
                    Process? gameProcess = GetGameProcess(gameProcessName);
                    if (gameProcess != null)
                    {
                        try
                        {
                            IntPtr handle = gameProcess.MainWindowHandle;
                            if (handle != IntPtr.Zero)
                            {
                                clickAction(handle, clickType);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogService.LogError("Process might have exited or been disposed.", ex); 
                        }
                    }
                },
                null,
                0,
                (int)ClickSpeed);

            lock (_timers)
            {
                _timers[id] = timer;
            }
            return id;
        }

        public void Unregister(Guid id)
        {
            lock (_timers)
            {
                if (_timers.TryGetValue(id, out System.Threading.Timer? timerInfo))
                {
                    timerInfo.Dispose();
                    _ = _timers.Remove(id);

                    LogService.LogInfo($"Clicker unregistered with ID: {id}");
                }
            }
        }

        public void Dispose()
        {
            lock (_timers)
            {
                foreach (System.Threading.Timer timer in _timers.Values)
                {
                    timer.Dispose();
                }

                _timers.Clear();
            }

            LogService.LogInfo("Clicker service disposed.");
        }

        private void UpdateTimersInterval()
        {
            lock (_timers)
            {
                foreach (System.Threading.Timer timer in _timers.Values)
                {
                    _ = timer.Change(0, (int)ClickSpeed);
                }
            }

            LogService.LogInfo($"Click speed updated to: {ClickSpeed}ms");
        }
    }
}