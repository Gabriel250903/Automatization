using Automatization.Services;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Automatization.Hotkeys
{
    public static class GlobalHotKeyManager
    {
        private const int WmHotkey = 0x0312;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static bool _isInitialized = false;
        private static int _nextId;
        private static Dictionary<int, HotKey> IdToHotKeyMap = [];
        private static Dictionary<HotKey, int> HotKeyToIdMap = [];

        public static bool IsPaused { get; set; } = false;

        public static event Action<HotKey>? HotKeyPressed;

        public static void Initialize()
        {
            if (_isInitialized)
            {
                LogService.LogWarning("GlobalHotKeyManager already initialized.");
                return;
            }

            LogService.LogInfo("Initializing GlobalHotKeyManager.");
            ComponentDispatcher.ThreadFilterMessage += OnThreadFilterMessage;
            _isInitialized = true;
        }

        public static bool Register(HotKey hotKey)
        {
            if (!_isInitialized || hotKey.IsEmpty || HotKeyToIdMap.ContainsKey(hotKey))
            {
                LogService.LogWarning($"Skipping registration for hotkey {hotKey}. Manager not initialized, hotkey is empty, or hotkey is already mapped.");
                return false;
            }

            int vk = KeyInterop.VirtualKeyFromKey(hotKey.Key);
            uint fsModifiers = (uint)hotKey.Modifiers;

            int id = _nextId++;
            if (!RegisterHotKey(IntPtr.Zero, id, fsModifiers, (uint)vk))
            {
                LogService.LogWarning($"Failed to register hotkey: {hotKey}");
                return false;
            }

            IdToHotKeyMap[id] = hotKey;
            HotKeyToIdMap[hotKey] = id;
            LogService.LogInfo($"Registered hotkey: {hotKey}");
            return true;
        }

        public static bool TryUnregister(HotKey hotKey)
        {
            if (!_isInitialized || !HotKeyToIdMap.TryGetValue(hotKey, out int id))
            {
                LogService.LogWarning($"Failed to unregister hotkey {hotKey}: Not found in map or manager not initialized.");
                return false;
            }

            if (UnregisterHotKey(IntPtr.Zero, id))
            {
                LogService.LogInfo($"Successfully unregistered hotkey from OS: {hotKey}");
            }
            else
            {
                LogService.LogWarning($"Failed to unregister hotkey from OS: {hotKey}");
            }

            _ = IdToHotKeyMap.Remove(id);
            _ = HotKeyToIdMap.Remove(hotKey);
            LogService.LogInfo($"Unregistered hotkey from manager: {hotKey}");
            return true;
        }

        public static void UnregisterAll()
        {
            if (!_isInitialized)
            {
                return;
            }

            LogService.LogInfo("Unregistering all hotkeys.");

            foreach (KeyValuePair<int, HotKey> entry in IdToHotKeyMap.ToList())
            {
                if (UnregisterHotKey(IntPtr.Zero, entry.Key))
                {
                    LogService.LogInfo($"Successfully unregistered hotkey from OS: {entry.Value}");
                }
                else
                {
                    LogService.LogWarning($"Failed to unregister hotkey from OS: {entry.Value}");
                }
            }

            IdToHotKeyMap.Clear();
            HotKeyToIdMap.Clear();
            LogService.LogInfo("All hotkeys unregistered from manager.");
        }

        public static IEnumerable<HotKey> GetCurrentRegisteredHotkeys()
        {
            return HotKeyToIdMap.Keys.ToList();
        }

        private static void OnThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (handled || IsPaused || msg.message != WmHotkey)
            {
                return;
            }

            int id = msg.wParam.ToInt32();
            if (IdToHotKeyMap.TryGetValue(id, out HotKey? hotKey))
            {
                LogService.LogInfo($"Hotkey pressed: {hotKey}");
                HotKeyPressed?.Invoke(hotKey);
                handled = true;
            }
        }

        public static void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            LogService.LogInfo("Shutting down GlobalHotKeyManager.");

            UnregisterAll();

            ComponentDispatcher.ThreadFilterMessage -= OnThreadFilterMessage;
            _isInitialized = false;
        }
    }
}
