using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Point = System.Windows.Point;

namespace Automatization.Utils
{
    public static class CursorUtils
    {
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
    }
}