using Automatization.Utils;

namespace Automatization.Services
{
    public class ScreenCaptureService
    {
        public static Bitmap CaptureScreen()
        {
            int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

            IntPtr hDesktopDC = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());
            IntPtr hMemoryDC = NativeMethods.CreateCompatibleDC(hDesktopDC);
            IntPtr hBitmap = NativeMethods.CreateCompatibleBitmap(hDesktopDC, width, height);
            IntPtr hOldBitmap = NativeMethods.SelectObject(hMemoryDC, hBitmap);

            _ = NativeMethods.BitBlt(hMemoryDC, 0, 0, width, height, hDesktopDC, 0, 0, NativeMethods.SRCCOPY);

            Bitmap bmp = Image.FromHbitmap(hBitmap);

            _ = NativeMethods.SelectObject(hMemoryDC, hOldBitmap);
            _ = NativeMethods.DeleteObject(hBitmap);
            _ = NativeMethods.DeleteDC(hMemoryDC);
            _ = NativeMethods.ReleaseDC(NativeMethods.GetDesktopWindow(), hDesktopDC);

            return bmp;
        }

        //public Bitmap CaptureRegion(Rectangle region)
        //{
        //    int width = region.Width;
        //    int height = region.Height;

        //    IntPtr hDesktopDC = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());
        //    IntPtr hMemoryDC = NativeMethods.CreateCompatibleDC(hDesktopDC);
        //    IntPtr hBitmap = NativeMethods.CreateCompatibleBitmap(hDesktopDC, width, height);
        //    IntPtr hOldBitmap = NativeMethods.SelectObject(hMemoryDC, hBitmap);

        //    NativeMethods.BitBlt(hMemoryDC, 0, 0, width, height, hDesktopDC, region.X, region.Y, NativeMethods.SRCCOPY);

        //    Bitmap bmp = Image.FromHbitmap(hBitmap);

        //    NativeMethods.SelectObject(hMemoryDC, hOldBitmap);
        //    NativeMethods.DeleteObject(hBitmap);
        //    NativeMethods.DeleteDC(hMemoryDC);
        //    NativeMethods.ReleaseDC(NativeMethods.GetDesktopWindow(), hDesktopDC);

        //    return bmp;
        //}
    }
}
