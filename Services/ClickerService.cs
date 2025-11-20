using Automatization.Settings;
using Automatization.Types;
using System.Windows.Threading;

namespace Automatization.Services
{
    public class ClickerService : IDisposable
    {
        private Dictionary<Guid, (DispatcherTimer Timer, ClickType ClickType)> _timers = [];
        private AppSettings _settings;

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

        public Guid Register(Action<ClickType> action, ClickType clickType)
        {
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromMilliseconds(ClickSpeed)
            };

            timer.Tick += async (s, e) => await Task.Run(() => action(clickType));
            timer.Start();

            Guid id = Guid.NewGuid();
            _timers[id] = (timer, clickType);

            LogService.LogInfo($"Clicker registered with ID: {id}");
            
            return id;
        }

        public void Unregister(Guid id)
        {
            if (_timers.TryGetValue(id, out (DispatcherTimer Timer, ClickType ClickType) timerInfo))
            {
                timerInfo.Timer.Stop();
                _timers.Remove(id);

                LogService.LogInfo($"Clicker unregistered with ID: {id}");
            }
        }

        public void Dispose()
        {
            foreach ((DispatcherTimer Timer, ClickType ClickType) timerInfo in _timers.Values)
            {
                timerInfo.Timer.Stop();
            }

            _timers.Clear();

            LogService.LogInfo("Clicker service disposed.");
        }

        private void UpdateTimersInterval()
        {
            TimeSpan newInterval = TimeSpan.FromMilliseconds(ClickSpeed);

            foreach ((DispatcherTimer Timer, ClickType ClickType) timerInfo in _timers.Values)
            {
                timerInfo.Timer.Interval = newInterval;
            }

            LogService.LogInfo($"Click speed updated to: {ClickSpeed}ms");
        }
    }
}