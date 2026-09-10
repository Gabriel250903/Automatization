using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Automatization.Services;

namespace Automatization.Hotkeys
{
    public class GlobalInputHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_XBUTTONDOWN = 0x020B;

        public event Func<Key, ModifierKeys, bool>? KeyDown;
        public event Func<MouseButton, ModifierKeys, bool>? MouseDown;

        private delegate IntPtr LowLevelHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr _kbdHook = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;
        private LowLevelHookProc _kbdProc;
        private LowLevelHookProc _mouseProc;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public int pt_x;
            public int pt_y;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelHookProc lpfn,
            IntPtr hMod,
            uint dwThreadId
        );

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        public GlobalInputHook()
        {
            _kbdProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;

            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule? curModule = curProcess.MainModule;

            if (curModule != null)
            {
                IntPtr hMod = GetModuleHandle(curModule.ModuleName);
                _kbdHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbdProc, hMod, 0);
                _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
            }
        }

        public void Dispose()
        {
            if (_kbdHook != IntPtr.Zero)
            {
                _ = UnhookWindowsHookEx(_kbdHook);
                _kbdHook = IntPtr.Zero;
            }

            if (_mouseHook != IntPtr.Zero)
            {
                _ = UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
        }

        private ModifierKeys GetCurrentModifiers()
        {
            return Keyboard.Modifiers;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
            {
                KBDLLHOOKSTRUCT kbStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                if ((kbStruct.flags & 0x10) == 0)
                {
                    Key key = KeyInterop.KeyFromVirtualKey((int)kbStruct.vkCode);
                    if (KeyDown != null)
                    {
                        bool handled = false;
                        foreach (
                            Func<Key, ModifierKeys, bool> handler in KeyDown
                                .GetInvocationList()
                                .Cast<Func<Key, ModifierKeys, bool>>()
                        )
                        {
                            try
                            {
                                if (handler(key, GetCurrentModifiers()))
                                {
                                    handled = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogService.LogError("Error in KeyboardHookCallback", ex);
                            }
                        }

                        if (handled)
                        {
                            return new IntPtr(1);
                        }
                    }
                }
            }

            return CallNextHookEx(_kbdHook, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN or WM_XBUTTONDOWN)
                {
                    MSLLHOOKSTRUCT mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    if ((mouseStruct.flags & 0x01) == 0)
                    {
                        MouseButton? button = null;

                        if (wParam == WM_LBUTTONDOWN)
                        {
                            button = MouseButton.Left;
                        }
                        else if (wParam == WM_RBUTTONDOWN)
                        {
                            button = MouseButton.Right;
                        }
                        else if (wParam == WM_MBUTTONDOWN)
                        {
                            button = MouseButton.Middle;
                        }
                        else if (wParam == WM_XBUTTONDOWN)
                        {
                            int xButton = (int)((mouseStruct.mouseData >> 16) & 0xFFFF);
                            if (xButton == 1)
                            {
                                button = MouseButton.XButton1;
                            }
                            else if (xButton == 2)
                            {
                                button = MouseButton.XButton2;
                            }
                        }

                        if (button.HasValue && MouseDown != null)
                        {
                            bool handled = false;
                            foreach (
                                Func<MouseButton, ModifierKeys, bool> handler in MouseDown
                                    .GetInvocationList()
                                    .Cast<Func<MouseButton, ModifierKeys, bool>>()
                            )
                            {
                                try
                                {
                                    if (handler(button.Value, GetCurrentModifiers()))
                                    {
                                        handled = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogService.LogError("Error in MouseHookCallback", ex);
                                }
                            }

                            if (handled)
                            {
                                return new IntPtr(1);
                            }
                        }
                    }
                }
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }
    }
}
