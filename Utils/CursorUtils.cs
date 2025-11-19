using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Point = System.Windows.Point;

namespace Automatization.Utils
{
    public static class CursorUtils
    {
        private static DispatcherTimer? _cursorTimer;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public static Point? GetCursorRelativeToProcess(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                return null;
            }

            IntPtr hWnd = processes[0].MainWindowHandle;
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            _ = GetCursorPos(out POINT p);
            _ = ScreenToClient(hWnd, ref p);

            return new Point(p.X, p.Y);
        }

        public static void InitializeCursorTracking(string processName)
        {
            _cursorTimer?.Stop();

            _cursorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _cursorTimer.Tick += (_, _) =>
            {
                Point? cursorPos = GetCursorRelativeToProcess(processName);
                if (cursorPos.HasValue)
                {
#if DEBUG
                    Debug.WriteLine($"Cursor: X={cursorPos.Value.X}, Y={cursorPos.Value.Y}");
#endif
                }
                else
                {
                    return;
                }
            };
            _cursorTimer.Start();
        }

        public static void StopCursorTracking()
        {
            _cursorTimer?.Stop();
            _cursorTimer = null;
        }
    }
}