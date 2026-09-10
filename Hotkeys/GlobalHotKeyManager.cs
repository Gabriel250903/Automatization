using System.Diagnostics;
using System.Windows.Input;
using Automatization.Services;
using Automatization.Settings;
using Automatization.Utils;
using Application = System.Windows.Application;

namespace Automatization.Hotkeys
{
    public static class GlobalHotKeyManager
    {
        private static AppSettings? _settings;
        private static bool _isInitialized = false;
        private static GlobalInputHook? _inputHook;
        private static readonly object _hotKeyLock = new();
        private static readonly HashSet<HotKey> _registeredHotKeys = [];
        public static bool IsPaused { get; set; } = false;
        public static event Action<HotKey, Process?>? HotKeyPressed;

        public static void Initialize()
        {
            if (_isInitialized)
            {
                LogService.LogWarning("GlobalHotKeyManager already initialized.");
                return;
            }

            _settings = App.Settings ?? AppSettings.Load();
            _inputHook = new GlobalInputHook();
            _inputHook.KeyDown += OnKeyDown;
            _inputHook.MouseDown += OnMouseDown;
            _isInitialized = true;

            LogService.LogInfo("Initialized GlobalHotKeyManager with low-level hooks.");
        }

        public static bool Register(HotKey hotKey)
        {
            if (!_isInitialized)
            {
                LogService.LogWarning(
                    $"Skipping registration for hotkey {hotKey}. Manager not initialized."
                );
                return false;
            }

            if (hotKey.IsEmpty)
            {
                return false;
            }

            lock (_hotKeyLock)
            {
                if (_registeredHotKeys.Contains(hotKey))
                {
                    LogService.LogWarning(
                        $"Skipping registration for hotkey {hotKey}. Hotkey is already mapped."
                    );
                    return false;
                }

                _ = _registeredHotKeys.Add(hotKey);
            }

            LogService.LogInfo($"Registered hotkey: {hotKey}");
            return true;
        }

        public static bool Unregister(HotKey hotKey)
        {
            if (!_isInitialized || hotKey.IsEmpty)
            {
                return false;
            }

            lock (_hotKeyLock)
            {
                if (_registeredHotKeys.Remove(hotKey))
                {
                    LogService.LogInfo($"Successfully unregistered hotkey: {hotKey}");
                    return true;
                }
            }

            return false;
        }

        public static void UnregisterAll()
        {
            if (!_isInitialized)
            {
                return;
            }

            lock (_hotKeyLock)
            {
                LogService.LogInfo(
                    $"Unregistering all hotkeys. Currently {_registeredHotKeys.Count} hotkeys registered."
                );
                _registeredHotKeys.Clear();
            }
        }

        private static bool ProcessHotKey(HotKey currentEvent)
        {
            if (IsPaused)
            {
                return false;
            }

            HotKey? matchedHotKey;
            lock (_hotKeyLock)
            {
                matchedHotKey = _registeredHotKeys.FirstOrDefault(hk => hk.Equals(currentEvent));
            }

            if (matchedHotKey == null)
            {
                return false;
            }

            Task.Run(() =>
                {
                    Process[] processes = Process.GetProcessesByName(
                        _settings?.GameProcessName ?? "ProTanki"
                    );
                    Process? game = null;

                    if (processes.Length > 0)
                    {
                        game = processes[0];
                        for (int i = 1; i < processes.Length; i++)
                        {
                            processes[i].Dispose();
                        }

                        if (!WindowUtils.IsGameWindowInForeground(game))
                        {
                            game.Dispose();
                            game = null;
                        }
                    }

                    _ = (
                        Application.Current?.Dispatcher.InvokeAsync(() =>
                        {
                            HotKeyPressed?.Invoke(matchedHotKey, game);
                            LogService.LogInfo($"Hotkey pressed: {matchedHotKey}");
                        })
                    );
                })
                .SafeFireAndForget("GlobalHotKeyManager.OnHotKey");

            return true;
        }

        private static bool OnKeyDown(Key key, ModifierKeys modifiers)
        {
            HotKey hk = new(key, modifiers);
            return ProcessHotKey(hk);
        }

        private static bool OnMouseDown(MouseButton button, ModifierKeys modifiers)
        {
            HotKey hk = new(button, modifiers);
            return ProcessHotKey(hk);
        }

        public static void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            UnregisterAll();

            if (_inputHook != null)
            {
                _inputHook.KeyDown -= OnKeyDown;
                _inputHook.MouseDown -= OnMouseDown;
                _inputHook.Dispose();
                _inputHook = null;
            }

            _isInitialized = false;
            LogService.LogInfo("Shutting down GlobalHotKeyManager.");
        }
    }
}
