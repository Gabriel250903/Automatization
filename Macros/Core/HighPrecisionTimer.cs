using System.Diagnostics;
using Automatization.Utils;

namespace Automatization.Macros.Core
{
    public static class HighPrecisionTimer
    {
        private static readonly double TicksPerMillisecond = Stopwatch.Frequency / 1000.0;
        private static int _timerPeriodRef = 0;
        private static readonly object _lock = new();

        public static void EnableHighResolution()
        {
            lock (_lock)
            {
                if (_timerPeriodRef == 0)
                {
                    _ = NativeMethods.TimeBeginPeriod(1);
                }
                _timerPeriodRef++;
            }
        }

        public static void DisableHighResolution()
        {
            lock (_lock)
            {
                if (_timerPeriodRef > 0)
                {
                    _timerPeriodRef--;
                    if (_timerPeriodRef == 0)
                    {
                        _ = NativeMethods.TimeEndPeriod(1);
                    }
                }
            }
        }

        public static void Delay(double milliseconds, CancellationToken cancellationToken = default)
        {
            if (milliseconds <= 0)
            {
                return;
            }

            long startTicks = Stopwatch.GetTimestamp();
            long targetTicks = startTicks + (long)(milliseconds * TicksPerMillisecond);

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                long currentTicks = Stopwatch.GetTimestamp();
                long remainingTicks = targetTicks - currentTicks;

                if (remainingTicks <= 0)
                {
                    break;
                }

                double remainingMs = remainingTicks / TicksPerMillisecond;

                if (remainingMs > 15.0)
                {
                    Thread.Sleep((int)(remainingMs - 10.0));
                }
                else if (remainingMs > 2.0)
                {
                    Thread.Sleep(1);
                }
                else if (remainingMs > 0.1)
                {
                    _ = Thread.Yield();
                }
                else
                {
                    Thread.SpinWait(10);
                }
            }
        }

        public static void DelayWithJitter(
            double minMs,
            double maxMs,
            CancellationToken cancellationToken = default
        )
        {
            if (minMs >= maxMs)
            {
                Delay(minMs, cancellationToken);
                return;
            }

            double chosenDelay = minMs + (Random.Shared.NextDouble() * (maxMs - minMs));
            Delay(chosenDelay, cancellationToken);
        }
    }
}
