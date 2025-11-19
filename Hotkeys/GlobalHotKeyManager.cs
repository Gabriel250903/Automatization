using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Automatization.Hotkeys
{
    public static class GlobalHotKeyManager
    {
        private const int WmHotkey = 0x0312;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static HwndSource? _source;
        private static int _nextId;
        private static Dictionary<int, HotKey> IdToHotKeyMap = [];
        private static Dictionary<HotKey, int> HotKeyToIdMap = [];

        public static bool IsPaused { get; set; } = false;

        public static event Action<HotKey>? HotKeyPressed;

        public static void Initialize(Window window)
        {
            if (_source != null)
            {
                return;
            }

            nint windowHandle = new WindowInteropHelper(window).Handle;
            _source = HwndSource.FromHwnd(windowHandle);
            _source?.AddHook(WndProc);
        }

        public static bool Register(HotKey hotKey)
        {
            if (_source == null || hotKey.IsEmpty || HotKeyToIdMap.ContainsKey(hotKey))
            {
                return false;
            }

            int vk = KeyInterop.VirtualKeyFromKey(hotKey.Key);
            uint fsModifiers = (uint)hotKey.Modifiers;

            int id = _nextId++;
            if (!RegisterHotKey(_source.Handle, id, fsModifiers, (uint)vk))
            {
                return false;
            }

            IdToHotKeyMap[id] = hotKey;
            HotKeyToIdMap[hotKey] = id;
            return true;
        }

        public static void Unregister(HotKey hotKey)
        {
            if (_source == null || !HotKeyToIdMap.TryGetValue(hotKey, out int id))
            {
                return;
            }

            _ = UnregisterHotKey(_source.Handle, id);
            _ = IdToHotKeyMap.Remove(id);
            _ = HotKeyToIdMap.Remove(hotKey);
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (IsPaused || msg != WmHotkey)
            {
                return IntPtr.Zero;
            }

            int id = wParam.ToInt32();
            if (IdToHotKeyMap.TryGetValue(id, out HotKey? hotKey))
            {
                HotKeyPressed?.Invoke(hotKey);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public static void Shutdown()
        {
            if (_source == null)
            {
                return;
            }

            foreach (int id in IdToHotKeyMap.Keys)
            {
                _ = UnregisterHotKey(_source.Handle, id);
            }

            IdToHotKeyMap.Clear();
            HotKeyToIdMap.Clear();

            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
    }
}
