using Automatization.Settings;
using Automatization.Types;
using Automatization.Utils;
using System.Diagnostics;
using System.Windows.Input;

namespace Automatization.Services
{
    public class SmartRepairKitService : IDisposable
    {
        public HealthBarDetector Detector { get; }
        private readonly ScreenCaptureService _captureService;

        private CancellationTokenSource? _cts;
        private bool _isRunning;

        private readonly object _processLock = new();
        private Process? _gameProcess;

        public string GameProcessName { get; set; } = "ProTanki";
        public int HealthThreshold { get; set; } = 40;
        public int CooldownMs { get; set; } = 5000;
        public Key ActivationKey { get; set; } = Key.D1;
        public int TargetFps { get; set; } = 60;
        private DateTime _lastActivationTime = DateTime.MinValue;
        public event Action<HealthBarStruct>? OnStateUpdated;
        public event Action? OnActivated;

        public SmartRepairKitService()
        {
            Detector = new HealthBarDetector();
            _captureService = new ScreenCaptureService();

            try
            {
                AppSettings settings = AppSettings.Load();
                GameProcessName = settings.GameProcessName;
                HealthThreshold = settings.SmartRepairThreshold;
                CooldownMs = settings.SmartRepairCooldown;
                ActivationKey = settings.SmartRepairKey;
                TargetFps = settings.SmartRepairFps;
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to load settings in SmartRepairKitService constructor: {ex.Message}.");
            }
        }

        public void UpdateColors(Color bright, Color dark)
        {
            Detector.SetCustomColors(bright, dark);
        }

        public void Start()
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => Loop(_cts.Token));
            LogService.LogInfo("Smart Repair Kit monitoring started.");
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            _cts?.Cancel();
            lock (_processLock)
            {
                _gameProcess?.Dispose();
                _gameProcess = null;
            }
            LogService.LogInfo("Smart Repair Kit monitoring stopped.");
        }

        private async Task Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    long startTime = DateTime.Now.Ticks;

                    using (Bitmap frame = _captureService.Capture())
                    {
                        HealthBarStruct state = Detector.Detect(frame);

                        OnStateUpdated?.Invoke(state);

                        if (state.IsFound)
                        {
                            if (state.HealthPercentage <= HealthThreshold)
                            {
                                TryActivateRepair();
                            }
                        }
                    }

                    int targetDelay = 1000 / Math.Max(1, TargetFps);
                    int elapsedMs = (int)((DateTime.Now.Ticks - startTime) / 10000);
                    int delay = Math.Max(1, targetDelay - elapsedMs);
                    await Task.Delay(delay, token);
                }
                catch (Exception ex)
                {
                    LogService.LogError($"Error in SmartRepairKit loop: {ex.Message}");
                    await Task.Delay(1000, token);
                }
            }
        }

        private void RefreshGameProcess()
        {
            lock (_processLock)
            {
                if (_gameProcess == null || HasProcessExited(_gameProcess))
                {
                    _gameProcess?.Dispose();
                    Process[] processes = Process.GetProcessesByName(GameProcessName);
                    if (processes.Length > 0)
                    {
                        _gameProcess = processes[0];
                        for (int i = 1; i < processes.Length; i++)
                        {
                            processes[i].Dispose();
                        }
                    }
                    else
                    {
                        _gameProcess = null;
                    }
                }
            }
        }

        private static bool HasProcessExited(Process process)
        {
            try
            {
                return process.HasExited;
            }
            catch
            {
                return true;
            }
        }

        private void TryActivateRepair()
        {
            if ((DateTime.Now - _lastActivationTime).TotalMilliseconds < CooldownMs)
            {
                return;
            }

            LogService.LogInfo($"Low health detected! Activating Repair Kit (Key: {ActivationKey})");

            int vKey = KeyInterop.VirtualKeyFromKey(ActivationKey);
            RefreshGameProcess();
            SimulateKeyPress((ushort)vKey, _gameProcess);

            _lastActivationTime = DateTime.Now;
            OnActivated?.Invoke();
        }

        private static void SimulateKeyPress(ushort virtualKey, Process? process)
        {
            if (process == null || process.MainWindowHandle == IntPtr.Zero)
            {
                LogService.LogWarning("Game process not found or has no main window handle. Cannot send key press.");
                return;
            }

            uint scanCode = NativeMethods.MapVirtualKey(virtualKey, 0);

            IntPtr lParamDown = (IntPtr)((scanCode << 16) | 1);
            IntPtr lParamUp = (IntPtr)((scanCode << 16) | 0xC0000001);

            _ = NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_KEYDOWN, virtualKey, lParamDown);
            _ = NativeMethods.PostMessage(process.MainWindowHandle, NativeMethods.WM_KEYUP, virtualKey, lParamUp);

            LogService.LogInfo($"Sent key press {virtualKey} (ScanCode: {scanCode}) to process {process.Id}");
        }

        public void ForceTrigger()
        {
            int vKey = KeyInterop.VirtualKeyFromKey(ActivationKey);
            RefreshGameProcess();
            SimulateKeyPress((ushort)vKey, _gameProcess);
            OnActivated?.Invoke();
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            _captureService?.Dispose();
            lock (_processLock)
            {
                _gameProcess?.Dispose();
                _gameProcess = null;
            }
        }
    }
}
