using Automatization.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Automatization.Utils
{
    public static class WindowUtils
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        private static int _foregroundCheckFailCount = 0;
        private const int MAX_LOG_COUNT = 5;

        public static bool IsGameWindowInForeground(Process? gameProcess)
        {
            if (gameProcess == null || gameProcess.HasExited)
            {
                return false;
            }

            IntPtr foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            _ = GetWindowThreadProcessId(foregroundWindowHandle, out uint foregroundProcessId);
            bool isInForeground = foregroundProcessId == gameProcess.Id;

            if (isInForeground)
            {
                _foregroundCheckFailCount = 0;
            }
            else
            {
                if (_foregroundCheckFailCount < MAX_LOG_COUNT)
                {
                    LogService.LogInfo($"Foreground check failed: Foreground process ID is {foregroundProcessId}, game process ID is {gameProcess.Id}.");
                    _foregroundCheckFailCount++;
                }
            }

            return isInForeground;
        }

        public static bool IsGameReadyForInput(Process? gameProcess)
        {
            if (!IsGameWindowInForeground(gameProcess))
            {
                return false;
            }

            IntPtr foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            uint threadId = GetWindowThreadProcessId(foregroundWindowHandle, out _);
            if (threadId == 0)
            {
                return true;
            }

            GUITHREADINFO gui = new()
            {
                cbSize = Marshal.SizeOf<GUITHREADINFO>()
            };

            if (GetGUIThreadInfo(threadId, ref gui))
            {
                IntPtr focusedControlHandle = gui.hwndFocus;
                if (focusedControlHandle == IntPtr.Zero)
                {
                    return true;
                }

                StringBuilder className = new(256);
                _ = GetClassName(focusedControlHandle, className, className.Capacity);

                return !className.ToString().Equals("EDIT", StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }
    }
}
