using Automatization.Services;
using Automatization.Settings;
using System.Runtime.InteropServices;
using System.Diagnostics;
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

        private static AppSettings? _settings;
        private static bool _isInitialized = false;
        private static int _nextId;
        private static Dictionary<int, HotKey> IdToHotKeyMap = [];
        private static Dictionary<HotKey, int> HotKeyToIdMap = [];

        public static bool IsPaused { get; set; } = false;

        public static event Action<HotKey, Process?>? HotKeyPressed;

        public static void Initialize()
        {
            if (_isInitialized)
            {
                LogService.LogWarning("GlobalHotKeyManager already initialized.");
                return;
            }

            ComponentDispatcher.ThreadFilterMessage += OnThreadFilterMessage;
            _settings = AppSettings.Load();
            _isInitialized = true;

            LogService.LogInfo("Initialized GlobalHotKeyManager.");
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
            bool success = RegisterHotKey(IntPtr.Zero, id, fsModifiers, (uint)vk);

            if (success)
            {
                IdToHotKeyMap[id] = hotKey;
                HotKeyToIdMap[hotKey] = id;
                LogService.LogInfo($"Registered hotkey: {hotKey} with ID {id}");
            }
            else
            {
                LogService.LogWarning($"Failed to register hotkey: {hotKey}. Error Code: {Marshal.GetLastWin32Error()}");
            }

            return success;
        }

        public static bool TryUnregister(HotKey hotKey)
        {
            if (!_isInitialized || !HotKeyToIdMap.TryGetValue(hotKey, out int id))
            {
                LogService.LogWarning($"Failed to unregister hotkey {hotKey}: Not found in map or manager not initialized.");
                return false;
            }

            bool success = UnregisterHotKey(IntPtr.Zero, id);
            if (success)
            {
                LogService.LogInfo($"Successfully unregistered hotkey from OS: {hotKey} with ID {id}");
            }
            else
            {
                LogService.LogWarning($"Failed to unregister hotkey from OS: {hotKey}. Error Code: {Marshal.GetLastWin32Error()}");
            }

            _ = IdToHotKeyMap.Remove(id);
            _ = HotKeyToIdMap.Remove(hotKey);

            LogService.LogInfo($"Unregistered hotkey from manager: {hotKey}");
            return success;
        }

        public static void UnregisterAll()
        {
            if (!_isInitialized)
            {
                return;
            }

            LogService.LogInfo($"Unregistering all hotkeys. Currently {IdToHotKeyMap.Count} hotkeys registered.");

            foreach (KeyValuePair<int, HotKey> entry in IdToHotKeyMap.ToList())
            {
                bool success = UnregisterHotKey(IntPtr.Zero, entry.Key);
                if (success)
                {
                    LogService.LogInfo($"Successfully unregistered hotkey from OS: {entry.Value} with ID {entry.Key}");
                }
                else
                {
                    LogService.LogWarning($"Failed to unregister hotkey from OS: {entry.Value} with ID {entry.Key}. Error Code: {Marshal.GetLastWin32Error()}");
                }
            }

            IdToHotKeyMap.Clear();
            HotKeyToIdMap.Clear();

            LogService.LogInfo($"All hotkeys unregistered from manager. Remaining: {IdToHotKeyMap.Count}");
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

            // Only process hotkeys if the game is in the foreground.
            // This prevents number keys from being swallowed by this app when typing elsewhere.
            Process? game = Process.GetProcessesByName(_settings?.GameProcessName ?? "ProTanki").FirstOrDefault();
            if (game == null || Utils.WindowUtils.IsGameWindowInForeground(game) == false)
            {
                // We received the hotkey, but we will not handle it, allowing other apps to use it.
                handled = false;
                return;
            }

            int id = msg.wParam.ToInt32();
            if (IdToHotKeyMap.TryGetValue(id, out HotKey? hotKey))
            {
                HotKeyPressed?.Invoke(hotKey, game);
                handled = true;

                LogService.LogInfo($"Hotkey pressed: {hotKey}");
            }
        }

        public static void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            UnregisterAll();

            ComponentDispatcher.ThreadFilterMessage -= OnThreadFilterMessage;
            _isInitialized = false;

            LogService.LogInfo("Shutting down GlobalHotKeyManager.");
        }
    }
}
