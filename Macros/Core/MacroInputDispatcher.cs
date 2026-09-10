using System.Runtime.InteropServices;
using Automatization.Utils;

namespace Automatization.Macros.Core
{
    public class MacroInputDispatcher(KeyTracker tracker)
    {
        private static readonly int InputStructSize = Marshal.SizeOf<NativeMethods.INPUT>();
        private readonly NativeMethods.INPUT[] _singleInputBuffer = new NativeMethods.INPUT[1];
        private readonly NativeMethods.INPUT[] _doubleInputBuffer = new NativeMethods.INPUT[2];
        private readonly object _inputLock = new();

        public KeyTracker Tracker { get; } = tracker;

        #region Keyboard Dispatching

        private static bool IsExtendedKey(ushort vk)
        {
            return vk switch
            {
                0x21 or 0x22 => true,
                0x23 or 0x24 => true,
                0x25 or 0x26 or 0x27 or 0x28 => true,
                0x2C or 0x2D or 0x2E => true,
                0x5B or 0x5C or 0x5D => true,
                0x6F => true,
                0x90 => true,
                0xA3 or 0xA5 => true,
                _ => false,
            };
        }

        private static bool ShouldUseSendInput(MacroTargetMode targetMode, IntPtr targetHwnd)
        {
            return targetMode == MacroTargetMode.Global
                || targetHwnd == IntPtr.Zero
                || NativeMethods.GetForegroundWindow() == targetHwnd;
        }

        public void SendKeyDown(ushort virtualKey, MacroTargetMode targetMode, IntPtr targetHwnd)
        {
            uint scanCode = NativeMethods.MapVirtualKey(virtualKey, 0);
            bool isExtended = IsExtendedKey(virtualKey);

            if (ShouldUseSendInput(targetMode, targetHwnd))
            {
                lock (_inputLock)
                {
                    uint flags = isExtended ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0;
                    ushort vkToSend = 0;
                    ushort scanToSend = 0;

                    if (scanCode != 0)
                    {
                        flags |= NativeMethods.KEYEVENTF_SCANCODE;
                        scanToSend = (ushort)scanCode;
                    }
                    else
                    {
                        vkToSend = virtualKey;
                    }

                    _singleInputBuffer[0].type = NativeMethods.INPUT_KEYBOARD;
                    _singleInputBuffer[0].U.ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = vkToSend,
                        wScan = scanToSend,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero,
                    };
                    _ = NativeMethods.SendInput(1, _singleInputBuffer, InputStructSize);
                }
            }
            else
            {
                IntPtr lParamDown = (IntPtr)((scanCode << 16) | 1);
                _ = NativeMethods.PostMessage(
                    targetHwnd,
                    NativeMethods.WM_KEYDOWN,
                    virtualKey,
                    lParamDown
                );
            }

            Tracker.RecordKeyDown(virtualKey, scanCode, targetHwnd, isExtended, targetMode);
        }

        public void SendKeyUp(ushort virtualKey, MacroTargetMode targetMode, IntPtr targetHwnd)
        {
            uint scanCode = NativeMethods.MapVirtualKey(virtualKey, 0);
            bool isExtended = IsExtendedKey(virtualKey);

            if (ShouldUseSendInput(targetMode, targetHwnd))
            {
                lock (_inputLock)
                {
                    uint flags =
                        NativeMethods.KEYEVENTF_KEYUP
                        | (isExtended ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0);
                    ushort vkToSend = 0;
                    ushort scanToSend = 0;

                    if (scanCode != 0)
                    {
                        flags |= NativeMethods.KEYEVENTF_SCANCODE;
                        scanToSend = (ushort)scanCode;
                    }
                    else
                    {
                        vkToSend = virtualKey;
                    }

                    _singleInputBuffer[0].type = NativeMethods.INPUT_KEYBOARD;
                    _singleInputBuffer[0].U.ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = vkToSend,
                        wScan = scanToSend,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero,
                    };
                    _ = NativeMethods.SendInput(1, _singleInputBuffer, InputStructSize);
                }
            }
            else
            {
                IntPtr lParamUp = (IntPtr)((scanCode << 16) | 0xC0000001);
                _ = NativeMethods.PostMessage(
                    targetHwnd,
                    NativeMethods.WM_KEYUP,
                    virtualKey,
                    lParamUp
                );
            }

            Tracker.RecordKeyUp(virtualKey);
        }

        public void SendKeyPress(
            ushort virtualKey,
            double holdDurationMs,
            MacroTargetMode targetMode,
            IntPtr targetHwnd,
            CancellationToken cancellationToken = default
        )
        {
            SendKeyDown(virtualKey, targetMode, targetHwnd);
            if (holdDurationMs > 0)
            {
                HighPrecisionTimer.Delay(holdDurationMs, cancellationToken);
            }
            SendKeyUp(virtualKey, targetMode, targetHwnd);
        }

        #endregion

        #region Mouse Dispatching

        public void SendMouseDown(
            MacroMouseButton button,
            int? x,
            int? y,
            MacroTargetMode targetMode,
            IntPtr targetHwnd
        )
        {
            if (x.HasValue && y.HasValue)
            {
                SendMouseMove(x.Value, y.Value, targetMode, targetHwnd);
            }

            if (ShouldUseSendInput(targetMode, targetHwnd))
            {
                uint flag = button switch
                {
                    MacroMouseButton.Right => NativeMethods.MOUSEEVENTF_RIGHTDOWN,
                    MacroMouseButton.Middle => NativeMethods.MOUSEEVENTF_MIDDLEDOWN,
                    _ => NativeMethods.MOUSEEVENTF_LEFTDOWN,
                };

                lock (_inputLock)
                {
                    _singleInputBuffer[0].type = NativeMethods.INPUT_MOUSE;
                    _singleInputBuffer[0].U.mi = new NativeMethods.MOUSEINPUT
                    {
                        dwFlags = flag,
                        dx = 0,
                        dy = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero,
                    };
                    _ = NativeMethods.SendInput(1, _singleInputBuffer, InputStructSize);
                }
            }
            else
            {
                uint msg = button switch
                {
                    MacroMouseButton.Right => NativeMethods.WM_RBUTTONDOWN,
                    MacroMouseButton.Middle => NativeMethods.WM_MBUTTONDOWN,
                    _ => NativeMethods.WM_LBUTTONDOWN,
                };

                IntPtr lParam =
                    x.HasValue && y.HasValue
                        ? NativeMethods.MakeLParam(x.Value, y.Value)
                        : IntPtr.Zero;
                _ = NativeMethods.PostMessage(targetHwnd, msg, IntPtr.Zero, lParam);
            }

            Tracker.RecordMouseDown(button, targetHwnd, targetMode);
        }

        public void SendMouseUp(
            MacroMouseButton button,
            int? x,
            int? y,
            MacroTargetMode targetMode,
            IntPtr targetHwnd
        )
        {
            if (ShouldUseSendInput(targetMode, targetHwnd))
            {
                uint flag = button switch
                {
                    MacroMouseButton.Right => NativeMethods.MOUSEEVENTF_RIGHTUP,
                    MacroMouseButton.Middle => NativeMethods.MOUSEEVENTF_MIDDLEUP,
                    _ => NativeMethods.MOUSEEVENTF_LEFTUP,
                };

                lock (_inputLock)
                {
                    _singleInputBuffer[0].type = NativeMethods.INPUT_MOUSE;
                    _singleInputBuffer[0].U.mi = new NativeMethods.MOUSEINPUT
                    {
                        dwFlags = flag,
                        dx = 0,
                        dy = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero,
                    };
                    _ = NativeMethods.SendInput(1, _singleInputBuffer, InputStructSize);
                }
            }
            else
            {
                uint msg = button switch
                {
                    MacroMouseButton.Right => NativeMethods.WM_RBUTTONUP,
                    MacroMouseButton.Middle => NativeMethods.WM_MBUTTONUP,
                    _ => NativeMethods.WM_LBUTTONUP,
                };

                IntPtr lParam =
                    x.HasValue && y.HasValue
                        ? NativeMethods.MakeLParam(x.Value, y.Value)
                        : IntPtr.Zero;
                _ = NativeMethods.PostMessage(targetHwnd, msg, IntPtr.Zero, lParam);
            }

            Tracker.RecordMouseUp(button);
        }

        public void SendMouseClick(
            MacroMouseButton button,
            int? x,
            int? y,
            double holdDurationMs,
            MacroTargetMode targetMode,
            IntPtr targetHwnd,
            CancellationToken cancellationToken = default
        )
        {
            SendMouseDown(button, x, y, targetMode, targetHwnd);
            if (holdDurationMs > 0)
            {
                HighPrecisionTimer.Delay(holdDurationMs, cancellationToken);
            }
            SendMouseUp(button, x, y, targetMode, targetHwnd);
        }

        public void SendMouseMove(int x, int y, MacroTargetMode targetMode, IntPtr targetHwnd)
        {
            if (ShouldUseSendInput(targetMode, targetHwnd))
            {
                _ = NativeMethods.SetCursorPos(x, y);
            }
            else
            {
                IntPtr lParam = NativeMethods.MakeLParam(x, y);
                _ = NativeMethods.PostMessage(
                    targetHwnd,
                    NativeMethods.WM_MOUSEMOVE,
                    IntPtr.Zero,
                    lParam
                );
            }
        }

        #endregion

        #region Stuck-Key Cleanup Sweep

        public void ReleaseAllTrackedInputs()
        {
            Tracker.ReleaseAll(
                releaseKeyAction: tracked =>
                {
                    if (ShouldUseSendInput(tracked.TargetMode, tracked.TargetHwnd))
                    {
                        lock (_inputLock)
                        {
                            uint flags =
                                NativeMethods.KEYEVENTF_KEYUP
                                | (tracked.IsExtended ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0);
                            ushort vkToSend = 0;
                            ushort scanToSend = 0;

                            if (tracked.ScanCode != 0)
                            {
                                flags |= NativeMethods.KEYEVENTF_SCANCODE;
                                scanToSend = (ushort)tracked.ScanCode;
                            }
                            else
                            {
                                vkToSend = tracked.VirtualKey;
                            }

                            _singleInputBuffer[0].type = NativeMethods.INPUT_KEYBOARD;
                            _singleInputBuffer[0].U.ki = new NativeMethods.KEYBDINPUT
                            {
                                wVk = vkToSend,
                                wScan = scanToSend,
                                dwFlags = flags,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero,
                            };
                            _ = NativeMethods.SendInput(1, _singleInputBuffer, InputStructSize);
                        }
                    }
                    else
                    {
                        IntPtr lParamUp = (IntPtr)((tracked.ScanCode << 16) | 0xC0000001);
                        _ = NativeMethods.PostMessage(
                            tracked.TargetHwnd,
                            NativeMethods.WM_KEYUP,
                            tracked.VirtualKey,
                            lParamUp
                        );
                    }
                },
                releaseMouseAction: tracked =>
                {
                    if (ShouldUseSendInput(tracked.TargetMode, tracked.TargetHwnd))
                    {
                        uint flag = tracked.Button switch
                        {
                            MacroMouseButton.Right => NativeMethods.MOUSEEVENTF_RIGHTUP,
                            MacroMouseButton.Middle => NativeMethods.MOUSEEVENTF_MIDDLEUP,
                            _ => NativeMethods.MOUSEEVENTF_LEFTUP,
                        };

                        lock (_inputLock)
                        {
                            _singleInputBuffer[0].type = NativeMethods.INPUT_MOUSE;
                            _singleInputBuffer[0].U.mi = new NativeMethods.MOUSEINPUT
                            {
                                dwFlags = flag,
                                dx = 0,
                                dy = 0,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero,
                            };
                            _ = NativeMethods.SendInput(1, _singleInputBuffer, InputStructSize);
                        }
                    }
                    else
                    {
                        uint msg = tracked.Button switch
                        {
                            MacroMouseButton.Right => NativeMethods.WM_RBUTTONUP,
                            MacroMouseButton.Middle => NativeMethods.WM_MBUTTONUP,
                            _ => NativeMethods.WM_LBUTTONUP,
                        };
                        _ = NativeMethods.PostMessage(
                            tracked.TargetHwnd,
                            msg,
                            IntPtr.Zero,
                            IntPtr.Zero
                        );
                    }
                }
            );
        }

        #endregion
    }
}
