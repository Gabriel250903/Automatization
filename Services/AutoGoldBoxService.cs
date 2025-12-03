using Automatization.Settings;

namespace Automatization.Services
{
    public class AutoGoldBoxService : IDisposable
    {
        private readonly ScreenCaptureService _captureService;
        private readonly GoldBoxDetector _detector;
        private readonly TextDetectorService _textDetectorService;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private bool _isNotificationVisible;
        private DateTime _lastNotificationTime = DateTime.MinValue;
        private const int NotificationCooldownMs = 2000;
        private const int NotificationClearanceDelayMs = 1500;

        public bool IsEnabled { get; set; } = false;
        public event Action? OnTriggered;

        public AutoGoldBoxService()
        {
            _captureService = new ScreenCaptureService();
            _textDetectorService = new TextDetectorService();
            _detector = new GoldBoxDetector(_textDetectorService);

            AppSettings settings = AppSettings.Load();
            IsEnabled = settings.EnableAutoGoldBox;

            if (!string.IsNullOrEmpty(settings.GoldBoxColor))
            {
                try
                {
                    _detector.TargetColor = ColorTranslator.FromHtml(settings.GoldBoxColor);
                }
                catch
                {
                    LogService.LogError($"[ADMIN-ONLY] Failed to set Gold Box color.");
                }
            }
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
            LogService.LogInfo("Auto Gold Box monitoring started.");
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            _cts?.Cancel();
            LogService.LogInfo("Auto Gold Box monitoring stopped.");
        }

        private async Task Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!IsEnabled)
                {
                    LogService.LogInfo("Auto Gold Box monitoring is disabled.");
                    await Task.Delay(1000, token);
                    continue;
                }

                try
                {
                    long startTime = DateTime.Now.Ticks;

                    using (Bitmap frame = ScreenCaptureService.CaptureScreen())
                    {
                        if (frame == null)
                        {
                            LogService.LogError("ScreenCaptureService returned a null frame.");
                            await Task.Delay(100, token);
                            continue;
                        }

                        Types.DetectionResultStruct detectionResult = _detector.DetectWithDetails(frame);

                        if (detectionResult.Success)
                        {
                            if (!_isNotificationVisible)
                            {
                                if ((DateTime.Now - _lastNotificationTime).TotalMilliseconds > NotificationCooldownMs)
                                {
                                    _isNotificationVisible = true;
                                    _lastNotificationTime = DateTime.Now;
                                    LogService.LogInfo($"Gold Box Notification Detected! OCR Text: '{detectionResult.DetectedText?.Trim() ?? "N/A"}'");
                                    OnTriggered?.Invoke();
                                }
                                else
                                {
                                    LogService.LogInfo($"Gold Box notification suppressed by cooldown. Last trigger: {_lastNotificationTime}");
                                }
                            }
                            else
                            {
                                _lastNotificationTime = DateTime.Now;
                                LogService.LogInfo("Gold Box notification still visible (debounce).");
                            }
                        }
                        else
                        {
                            if (_isNotificationVisible && (DateTime.Now - _lastNotificationTime).TotalMilliseconds > NotificationClearanceDelayMs)
                            {
                                _isNotificationVisible = false;
                                LogService.LogInfo("Gold Box Notification Cleared.");
                            }
                            else if (_isNotificationVisible)
                            {
                                LogService.LogInfo("Gold Box notification still within clearance delay.");
                            }
                            else
                            {
                                if (detectionResult.ColorDetected)
                                {
                                    LogService.LogInfo($"Gold Box color detected, but OCR failed or text not matched. OCR Text: '{detectionResult.DetectedText?.Trim() ?? "N/A"}', Error: {detectionResult.Error ?? "None"}");
                                }
                            }
                        }
                    }

                    int elapsedMs = (int)((DateTime.Now.Ticks - startTime) / 10000);
                    int delay = Math.Max(1, 100 - elapsedMs);
                    await Task.Delay(delay, token);
                }
                catch (Exception ex)
                {
                    LogService.LogError($"Error in AutoGoldBox loop: {ex.Message}");
                    await Task.Delay(1000, token);
                }
            }
        }

        public void UpdateSettings(bool enable, string colorHex)
        {
            IsEnabled = enable;
            try
            {
                _detector.TargetColor = ColorTranslator.FromHtml(colorHex);
            }
            catch
            {
                LogService.LogError($"[ADMIN-ONLY] Failed to update Gold Box color.");
            }
        }

        public void SimulateTrigger()
        {
            LogService.LogInfo("Simulating Gold Box trigger.");
            OnTriggered?.Invoke();
        }

        public string TestDetection(Bitmap bmp)
        {
            try
            {
                Types.DetectionResultStruct result = _detector.DetectWithDetails(bmp);
                if (result.Success)
                {
                    return "Gold Box detected!";
                }
                else
                {
                    string msg = "Failed.";
                    if (!result.ColorDetected)
                    {
                        msg += " Color NOT detected.";
                    }
                    else
                    {
                        msg += " Color detected.";
                    }

                    if (result.DetectedText != null)
                    {
                        msg += $" OCR Found: '{result.DetectedText.Trim()}'";
                    }
                    else
                    {
                        msg += " OCR Found: (null)";
                    }

                    if (result.Error != null)
                    {
                        msg += $" Error: {result.Error}";
                    }

                    return msg;
                }
            }
            catch (Exception ex)
            {
                return $"Error during detection: {ex.Message}";
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            _textDetectorService?.Dispose();
        }
    }
}
