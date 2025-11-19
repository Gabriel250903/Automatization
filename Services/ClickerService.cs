using System.Windows.Threading;
using Automatization.Settings;
using Automatization.Types;

namespace Automatization.Services
{
    public class ClickerService(AppSettings settings) : IDisposable
    {
        private readonly Dictionary<Guid, (DispatcherTimer Timer, ClickType ClickType)> _timers = [];
        private readonly AppSettings _settings = settings;

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
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ClickSpeed)
            };
            timer.Tick += (s, e) => action(clickType);
            timer.Start();

            var id = Guid.NewGuid();
            _timers[id] = (timer, clickType);
            return id;
        }

        public void Unregister(Guid id)
        {
            if (_timers.TryGetValue(id, out var timerInfo))
            {
                timerInfo.Timer.Stop();
                _timers.Remove(id);
            }
        }

        public void Dispose()
        {
            foreach (var timerInfo in _timers.Values)
            {
                timerInfo.Timer.Stop();
            }
            _timers.Clear();
        }

        private void UpdateTimersInterval()
        {
            var newInterval = TimeSpan.FromMilliseconds(ClickSpeed);
            foreach (var timerInfo in _timers.Values)
            {
                timerInfo.Timer.Interval = newInterval;
            }
        }
    }
}