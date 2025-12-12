using Automatization.Utils;
using SharpGen.Runtime;
using System.Drawing.Imaging;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Automatization.Services
{
    public class ScreenCaptureService : IDisposable
    {
        private ID3D11Device? _device;
        private ID3D11DeviceContext? _context;
        private IDXGIOutputDuplication? _duplication;
        private ID3D11Texture2D? _stagingTexture;
        private bool _initialized = false;

        public ScreenCaptureService()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                using IDXGIFactory1 factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();

                List<IDXGIAdapter1> adapters = [];
                List<ID3D11Device> devices = [];
                List<ID3D11DeviceContext> contexts = [];

                for (int i = 0; ; i++)
                {
                    Result result = factory.EnumAdapters1((uint)i, out IDXGIAdapter1? adapter);

                    if (result.Failure)
                    {
                        break;
                    }

                    adapters.Add(adapter);

                    try
                    {
                        D3D11.D3D11CreateDevice(
                            adapter,
                            DriverType.Unknown,
                            DeviceCreationFlags.BgraSupport,
                            null,
                            out ID3D11Device device,
                            out ID3D11DeviceContext context
                        ).CheckError();

                        devices.Add(device);
                        contexts.Add(context);
                        LogService.LogInfo($"Created D3D11 Device on Adapter {i}: {adapter.Description.Description}");
                    }
                    catch (Exception ex)
                    {
                        LogService.LogWarning($"Failed to create device on Adapter {i}: {ex.Message}");
                    }
                }

                foreach (IDXGIAdapter1 adapter in adapters)
                {
                    for (int j = 0; ; j++)
                    {
                        Result result = adapter.EnumOutputs((uint)j, out IDXGIOutput? output);
                        if (result.Failure || output == null)
                        {
                            break;
                        }

                        if (!output.Description.AttachedToDesktop)
                        {
                            output.Dispose();
                            continue;
                        }

                        using IDXGIOutput1 output1 = output.QueryInterface<IDXGIOutput1>();
                        output.Dispose();

                        LogService.LogInfo($"Testing Output {output1.Description.DeviceName} (found on {adapter.Description.Description})");

                        foreach (ID3D11Device device in devices)
                        {
                            try
                            {
                                _duplication = output1.DuplicateOutput(device);
                                _device = device;
                                _context = contexts[devices.IndexOf(device)];
                                _initialized = true;

                                LogService.LogInfo("Success! Desktop Duplication initialized.");

                                CleanupResources(adapters, devices, contexts, _device, _context);
                                return;
                            }
                            catch
                            {
                            }
                        }
                    }
                }

                CleanupResources(adapters, devices, contexts, null, null);
                LogService.LogWarning("Desktop Duplication not available (common on hybrid graphics). Falling back to GDI capture.");
            }
            catch (Exception ex)
            {
                LogService.LogWarning($"Desktop Duplication initialization failed: {ex.Message}. Falling back to GDI capture.");
                DisposeResources();
            }
        }

        private static void CleanupResources(
            List<IDXGIAdapter1> adapters,
            List<ID3D11Device> devices,
            List<ID3D11DeviceContext> contexts,
            ID3D11Device? keptDevice,
            ID3D11DeviceContext? keptContext)
        {
            foreach (ID3D11Device d in devices)
            {
                if (d != keptDevice)
                {
                    d.Dispose();
                }
            }
            foreach (ID3D11DeviceContext c in contexts)
            {
                if (c != keptContext)
                {
                    c.Dispose();
                }
            }
            foreach (IDXGIAdapter1 a in adapters)
            {
                a.Dispose();
            }
        }

        public Bitmap Capture()
        {
            if (!_initialized || _duplication == null || _device == null || _context == null)
            {
                return CaptureScreenGDI();
            }

            try
            {
                OutduplFrameInfo frameInfo = new();

                Result result = _duplication.AcquireNextFrame(100, out frameInfo, out IDXGIResource? desktopResource);

                if (result.Failure)
                {
                    if (result == Vortice.DXGI.ResultCode.WaitTimeout)
                    {
                        return CaptureScreenGDI();
                    }
                    else if (result == Vortice.DXGI.ResultCode.AccessLost)
                    {
                        LogService.LogInfo("Desktop Duplication access lost. Re-initializing...");
                        DisposeResources();
                        Initialize();
                        return CaptureScreenGDI();
                    }
                    else
                    {
                        return CaptureScreenGDI();
                    }
                }

                using ID3D11Texture2D texture = desktopResource!.QueryInterface<ID3D11Texture2D>();

                Texture2DDescription desc = texture.Description;
                if (_stagingTexture == null ||
                    _stagingTexture.Description.Width != desc.Width ||
                    _stagingTexture.Description.Height != desc.Height)
                {
                    _stagingTexture?.Dispose();
                    _stagingTexture = _device.CreateTexture2D(new Texture2DDescription
                    {
                        Width = desc.Width,
                        Height = desc.Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = Format.B8G8R8A8_UNorm,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Staging,
                        BindFlags = BindFlags.None,
                        CPUAccessFlags = CpuAccessFlags.Read,
                        MiscFlags = ResourceOptionFlags.None
                    });
                }

                _context.CopyResource(_stagingTexture, texture);

                _ = _duplication.ReleaseFrame();
                desktopResource.Dispose();

                MappedSubresource map = _context.Map(_stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

                try
                {
                    Bitmap bmp = new((int)desc.Width, (int)desc.Height, PixelFormat.Format32bppArgb);
                    BitmapData bmpData = bmp.LockBits(
                        new Rectangle(0, 0, (int)desc.Width, (int)desc.Height),
                        ImageLockMode.WriteOnly,
                        bmp.PixelFormat);

                    unsafe
                    {
                        byte* source = (byte*)map.DataPointer;
                        byte* dest = (byte*)bmpData.Scan0;
                        int h = (int)desc.Height;
                        int wBytes = (int)desc.Width * 4;

                        for (int y = 0; y < h; y++)
                        {
                            Buffer.MemoryCopy(source, dest, wBytes, wBytes);
                            source += map.RowPitch;
                            dest += bmpData.Stride;
                        }
                    }

                    bmp.UnlockBits(bmpData);
                    return bmp;
                }
                finally
                {
                    _context.Unmap(_stagingTexture, 0);
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"Capture failed: {ex.Message}");
                return CaptureScreenGDI();
            }
        }

        public static Bitmap CaptureScreenGDI()
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

        public static Bitmap CaptureScreen()
        {
            return CaptureScreenGDI();
        }

        private void DisposeResources()
        {
            _stagingTexture?.Dispose();
            _stagingTexture = null;
            _duplication?.Dispose();
            _duplication = null;
            _context?.Dispose();
            _context = null;
            _device?.Dispose();
            _device = null;
            _initialized = false;
        }

        public void Dispose()
        {
            DisposeResources();
            GC.SuppressFinalize(this);
        }
    }
}