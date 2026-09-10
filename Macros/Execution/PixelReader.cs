using System.Runtime.InteropServices;

namespace Automatization.Macros.Execution
{
    public static class PixelReader
    {
        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [ThreadStatic]
        private static ScopedSession? _threadSession;

        public static IDisposable CreateSession()
        {
            return new ScopedSession();
        }

        public sealed class ScopedSession : IDisposable
        {
            private bool _disposed;
            private readonly ScopedSession? _parent;

            public IntPtr Hdc { get; private set; }

            public ScopedSession()
            {
                _parent = _threadSession;
                Hdc = GetDC(IntPtr.Zero);
                _threadSession = this;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    if (Hdc != IntPtr.Zero)
                    {
                        _ = ReleaseDC(IntPtr.Zero, Hdc);
                        Hdc = IntPtr.Zero;
                    }
                    _threadSession = _parent;
                }
            }
        }

        public static uint GetScreenPixelColor(int x, int y)
        {
            if (_threadSession != null && _threadSession.Hdc != IntPtr.Zero)
            {
                return ReadColorFromHdc(_threadSession.Hdc, x, y);
            }

            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero)
            {
                return 0;
            }

            try
            {
                return ReadColorFromHdc(hdc, x, y);
            }
            finally
            {
                _ = ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        private static uint ReadColorFromHdc(IntPtr hdc, int x, int y)
        {
            uint pixel = GetPixel(hdc, x, y);
            if (pixel == 0xFFFFFFFF)
            {
                return 0;
            }

            uint r = pixel & 0x000000FF;
            uint g = (pixel & 0x0000FF00) >> 8;
            uint b = (pixel & 0x00FF0000) >> 16;
            return (r << 16) | (g << 8) | b;
        }

        public static bool ColorsMatch(uint c1, uint c2, int tolerance = 15)
        {
            if (tolerance <= 0)
            {
                return c1 == c2;
            }

            int r1 = (int)((c1 >> 16) & 0xFF);
            int g1 = (int)((c1 >> 8) & 0xFF);
            int b1 = (int)(c1 & 0xFF);

            int r2 = (int)((c2 >> 16) & 0xFF);
            int g2 = (int)((c2 >> 8) & 0xFF);
            int b2 = (int)(c2 & 0xFF);

            int dr = r1 - r2;
            int dg = g1 - g2;
            int db = b1 - b2;

            return ((dr * dr) + (dg * dg) + (db * db)) <= (tolerance * tolerance * 3);
        }
    }
}
