using Automatization.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Automatization.Utils
{
    public static class WindowUtils
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

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
            IntPtr gameWindowHandle = gameProcess.MainWindowHandle;

            bool isInForeground = foregroundWindowHandle == gameWindowHandle;

            if (isInForeground)
            {
                _foregroundCheckFailCount = 0;
            }
            else
            {
                if (_foregroundCheckFailCount < MAX_LOG_COUNT)
                {
                    LogService.LogInfo($"Foreground check failed: Foreground window handle is {foregroundWindowHandle}, game window handle is {gameWindowHandle}.");
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

            IntPtr focusedControlHandle = GetFocus();
            if (focusedControlHandle == IntPtr.Zero)
            {
                return true;
            }

            GetWindowThreadProcessId(focusedControlHandle, out uint focusedControlProcessId);
            if (gameProcess != null && focusedControlProcessId != gameProcess.Id)
            {
                return false;
            }

            StringBuilder className = new();
            GetClassName(focusedControlHandle, className, className.Capacity);

            return !className.ToString().Equals("EDIT", StringComparison.OrdinalIgnoreCase);
        }
    }
}
