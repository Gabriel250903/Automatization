using Automatization.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Automatization.Utils
{
    public static class WindowUtils
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static bool IsGameWindowInForeground(Process? gameProcess)
        {
            if (gameProcess == null || gameProcess.HasExited)
            {
                return false;
            }

            IntPtr foregroundWindowHandle = GetForegroundWindow();
            IntPtr gameWindowHandle = gameProcess.MainWindowHandle;

            bool isInForeground = foregroundWindowHandle == gameWindowHandle;

            if (!isInForeground)
            {
                LogService.LogInfo($"Foreground check failed: Foreground window handle is {foregroundWindowHandle}, game window handle is {gameWindowHandle}.");
            }

            return isInForeground;
        }
    }
}
