using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Automatization.Hotkeys;
using Automatization.Macros.Core;
using Automatization.Macros.Execution;
using Automatization.Macros.Models;
using Automatization.Utils;

namespace Automatization.Services
{
    public class MacroService : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };
        private readonly ConcurrentDictionary<Guid, MacroRunner> _runners = new();
        private readonly ConcurrentDictionary<HotKey, MacroDefinition> _hotKeyMap = new();
        private Func<Process?>? _getGameProcess;
        private Func<bool>? _isGameReadyForInput;

        public ObservableCollection<MacroDefinition> Macros { get; } = [];
        public KeyTracker KeyTracker { get; } = new();
        public MacroInputDispatcher Dispatcher { get; }

        public event Action? MacrosChanged;
        public event Action<MacroDefinition, MacroExecutionState>? MacroStateChanged;

        public MacroService()
        {
            Dispatcher = new MacroInputDispatcher(KeyTracker);
            LogService.LogInfo("MacroService created.");
        }

        public void Initialize(Func<Process?> getGameProcess, Func<bool> isGameReadyForInput)
        {
            _getGameProcess = getGameProcess;
            _isGameReadyForInput = isGameReadyForInput;

            LoadMacros();
            RegisterMacroHotkeys();

            LogService.LogInfo($"MacroService initialized with {Macros.Count} macros.");
        }

        private readonly HashSet<HotKey> _registeredHotKeys = [];

        public void RegisterMacroHotkeys()
        {
            _hotKeyMap.Clear();
            foreach (MacroDefinition macro in Macros)
            {
                if (macro.IsEnabled && !macro.TriggerHotKey.IsEmpty)
                {
                    _hotKeyMap[macro.TriggerHotKey] = macro;
                    if (_registeredHotKeys.Add(macro.TriggerHotKey))
                    {
                        _ = GlobalHotKeyManager.Register(macro.TriggerHotKey);
                    }
                }
            }
        }

        public void UnregisterMacroHotkeys()
        {
            _hotKeyMap.Clear();
            foreach (HotKey hotkey in _registeredHotKeys.ToList())
            {
                _ = GlobalHotKeyManager.Unregister(hotkey);
            }
            _registeredHotKeys.Clear();
        }

        public void ReRegisterMacroHotkeys()
        {
            UnregisterMacroHotkeys();
            RegisterMacroHotkeys();
        }

        private readonly ConcurrentDictionary<Guid, bool> _hotKeyAwaitingRelease = new();

        public bool HandleHotKey(HotKey hotkey)
        {
            if (!_hotKeyMap.TryGetValue(hotkey, out MacroDefinition? target) || !target.IsEnabled)
            {
                return false;
            }

            if (target.TriggerMode == MacroTriggerMode.Hold)
            {
                if (_runners.TryGetValue(target.Id, out MacroRunner? runner) && runner.IsRunning)
                {
                    return true;
                }

                StartMacro(target, isTestRun: false);
                return true;
            }

            if (
                _hotKeyAwaitingRelease.TryGetValue(target.Id, out bool awaitingRelease)
                && awaitingRelease
            )
            {
                return true;
            }

            _hotKeyAwaitingRelease[target.Id] = true;

            int vk = target.TriggerHotKey.VirtualKey;
            Task.Run(async () =>
                {
                    try
                    {
                        if (vk != 0)
                        {
                            while ((NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0)
                            {
                                await Task.Delay(25).ConfigureAwait(false);
                            }
                        }
                        await Task.Delay(150).ConfigureAwait(false);
                    }
                    catch { }
                    finally
                    {
                        _hotKeyAwaitingRelease[target.Id] = false;
                    }
                })
                .SafeFireAndForget("MacroService.ReleaseWatcher");

            ToggleMacro(target, isTestRun: false);
            return true;
        }

        public void ToggleMacro(MacroDefinition macro, bool isTestRun = false)
        {
            if (!_runners.TryGetValue(macro.Id, out MacroRunner? runner))
            {
                runner = new MacroRunner(
                    macro,
                    Dispatcher,
                    _getGameProcess ?? (() => WindowUtils.GetFirstProcessByName("ProTanki")),
                    _isGameReadyForInput ?? (() => true)
                );
                runner.StateChanged += (m, s) => MacroStateChanged?.Invoke(m, s);
                _runners[macro.Id] = runner;
            }

            if (runner.IsRunning)
            {
                runner.Stop();
            }
            else
            {
                StopAllRunningExcept(macro.Id);
                _ = runner.Start(isTestRun);
            }
        }

        public void StartMacro(MacroDefinition macro, bool isTestRun = false)
        {
            StopAllRunningExcept(macro.Id);

            if (!_runners.TryGetValue(macro.Id, out MacroRunner? runner))
            {
                runner = new MacroRunner(
                    macro,
                    Dispatcher,
                    _getGameProcess ?? (() => WindowUtils.GetFirstProcessByName("ProTanki")),
                    _isGameReadyForInput ?? (() => true)
                );
                runner.StateChanged += (m, s) => MacroStateChanged?.Invoke(m, s);
                _runners[macro.Id] = runner;
            }

            _ = runner.Start(isTestRun);
        }

        private void StopAllRunningExcept(Guid exceptMacroId)
        {
            foreach (KeyValuePair<Guid, MacroRunner> kvp in _runners)
            {
                if (kvp.Key != exceptMacroId && kvp.Value.IsRunning)
                {
                    kvp.Value.Stop();
                }
            }
        }

        public void StopMacro(MacroDefinition macro)
        {
            if (_runners.TryGetValue(macro.Id, out MacroRunner? runner))
            {
                runner.Stop();
            }
        }

        public bool HasHotKeyConflict(HotKey hotkey, Guid? currentMacroId = null)
        {
            return !hotkey.IsEmpty
                && Macros.Any(m =>
                    m.IsEnabled && m.Id != currentMacroId && m.TriggerHotKey.Equals(hotkey)
                );
        }

        public void StopAll()
        {
            LogService.LogInfo("Stopping all macros and releasing simulated keys.");

            foreach (MacroRunner runner in _runners.Values)
            {
                runner.Stop();
            }

            Dispatcher.ReleaseAllTrackedInputs();

            foreach (MacroDefinition macro in Macros)
            {
                if (macro.State is MacroExecutionState.Running or MacroExecutionState.Paused)
                {
                    macro.State = MacroExecutionState.Idle;
                    MacroStateChanged?.Invoke(macro, MacroExecutionState.Idle);
                }
            }
        }

        public void AddMacro(MacroDefinition macro)
        {
            Macros.Add(macro);
            if (macro.IsEnabled && !macro.TriggerHotKey.IsEmpty)
            {
                _hotKeyMap[macro.TriggerHotKey] = macro;
                if (_registeredHotKeys.Add(macro.TriggerHotKey))
                {
                    _ = GlobalHotKeyManager.Register(macro.TriggerHotKey);
                }
            }
            SaveMacros();
            MacrosChanged?.Invoke();
        }

        public void RemoveMacro(MacroDefinition macro)
        {
            StopMacro(macro);
            _ = _runners.TryRemove(macro.Id, out _);
            if (!macro.TriggerHotKey.IsEmpty)
            {
                _ = _hotKeyMap.TryRemove(macro.TriggerHotKey, out _);
            }
            _ = Macros.Remove(macro);
            SaveMacros();
            MacrosChanged?.Invoke();
        }

        #region Persistence

        public void LoadMacros()
        {
            try
            {
                string path = GetMacrosFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    List<MacroDefinition>? loaded = JsonSerializer.Deserialize<
                        List<MacroDefinition>
                    >(json, JsonOptions);
                    if (loaded != null)
                    {
                        Macros.Clear();
                        foreach (MacroDefinition m in loaded)
                        {
                            Macros.Add(m);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to load macros from disk.", ex);
            }
        }

        public void SaveMacros()
        {
            try
            {
                string path = GetMacrosFilePath();
                if (Macros.Count == 0)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    return;
                }

                string json = JsonSerializer.Serialize(Macros.ToList(), JsonOptions);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to save macros to disk.", ex);
            }
        }

        private static string GetMacrosFilePath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TankAutomation"
            );

            if (!Directory.Exists(dir))
            {
                _ = Directory.CreateDirectory(dir);
            }

            return Path.Combine(dir, "macros.json");
        }

        public static void ExportMacroToFile(MacroDefinition macro, string filePath)
        {
            MacroPackage package = new()
            {
                Magic = MacroPackage.Header,
                Version = 1,
                ExportedAt = DateTime.UtcNow,
                AppVersion = "2.2.3",
                Macro = macro,
            };

            string json = JsonSerializer.Serialize(package, JsonOptions);
            File.WriteAllText(filePath, json);
            LogService.LogInfo($"Exported macro '{macro.Name}' to {filePath}");
        }

        public MacroDefinition ImportMacroFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Macro file not found.", filePath);
            }

            string json = File.ReadAllText(filePath);
            MacroPackage? package = JsonSerializer.Deserialize<MacroPackage>(json, JsonOptions);

            if (package == null || package.Magic != MacroPackage.Header || package.Macro == null)
            {
                throw new InvalidDataException(
                    "The selected file is not a valid Automatization Macro package (.azmacro)."
                );
            }

            MacroDefinition imported = package.Macro;
            imported.Id = Guid.NewGuid();
            imported.State = MacroExecutionState.Idle;
            imported.RunCount = 0;
            imported.LastRunTime = null;

            string baseName = imported.Name;
            int counter = 1;
            while (
                Macros.Any(m => m.Name.Equals(imported.Name, StringComparison.OrdinalIgnoreCase))
            )
            {
                imported.Name = $"{baseName} ({counter++})";
            }

            AddMacro(imported);
            LogService.LogInfo($"Imported macro '{imported.Name}' from {filePath}");
            return imported;
        }

        #endregion

        public void Dispose()
        {
            StopAll();
            foreach (MacroRunner runner in _runners.Values)
            {
                runner.Dispose();
            }
            _runners.Clear();
        }
    }
}
