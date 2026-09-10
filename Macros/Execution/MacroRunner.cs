using System.Diagnostics;
using Automatization.Macros.Compiler;
using Automatization.Macros.Core;
using Automatization.Macros.Models;
using Automatization.Services;

namespace Automatization.Macros.Execution
{
    public class MacroRunner(
        MacroDefinition macro,
        MacroInputDispatcher inputDispatcher,
        Func<Process?> getGameProcess,
        Func<bool> isGameReadyForInput
    ) : IDisposable
    {
        private readonly MacroInputDispatcher _inputDispatcher = inputDispatcher;
        private readonly Func<Process?> _getGameProcess = getGameProcess;
        private readonly Func<bool> _isGameReadyForInput = isGameReadyForInput;

        private CancellationTokenSource? _cts;
        private Task? _runningTask;
        private readonly object _stateLock = new();

        public event Action<MacroDefinition, MacroExecutionState>? StateChanged;

        public MacroDefinition Macro { get; } = macro;
        public bool IsRunning =>
            Macro.State is MacroExecutionState.Running or MacroExecutionState.Paused;

        private void SetState(MacroExecutionState state)
        {
            Macro.State = state;
            StateChanged?.Invoke(Macro, state);
        }

        private bool _isTestRun;

        public bool Start(bool isTestRun = false)
        {
            lock (_stateLock)
            {
                if (IsRunning)
                {
                    return false;
                }

                _isTestRun = isTestRun;

                AutoScriptCompiler compiler = new();
                CompilationResult result = compiler.Compile(Macro.ScriptText, Macro);

                if (!result.Success || result.Instructions.Length == 0)
                {
                    SetState(MacroExecutionState.Error);
                    string errors = string.Join(
                        "; ",
                        result.Diagnostics.Select(d => $"{d.Message} (Line {d.Line})")
                    );
                    LogService.LogWarning($"Macro '{Macro.Name}' failed to compile: {errors}");
                    return false;
                }

                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;

                HighPrecisionTimer.EnableHighResolution();
                Macro.LastRunTime = DateTime.Now;
                SetState(MacroExecutionState.Running);

                _runningTask = Task.Run(() => ExecuteMacroLoop(result.Instructions, token), token);
                LogService.LogInfo(
                    $"Started macro '{Macro.Name}' (ID: {Macro.Id}, TestRun: {isTestRun})."
                );
                return true;
            }
        }

        public void Stop()
        {
            lock (_stateLock)
            {
                if (!IsRunning)
                {
                    return;
                }

                try
                {
                    _cts?.Cancel();
                }
                catch { }

                _inputDispatcher.ReleaseAllTrackedInputs();
                SetState(MacroExecutionState.Idle);
                LogService.LogInfo($"Stopped macro '{Macro.Name}'.");
            }
        }

        private void ExecuteMacroLoop(
            Instruction[] instructions,
            CancellationToken cancellationToken
        )
        {
            int maxCounter = 0;
            for (int i = 0; i < instructions.Length; i++)
            {
                if (
                    instructions[i].Opcode is Opcode.RepeatStart or Opcode.RepeatEnd
                    && instructions[i].CounterIndex > maxCounter
                )
                {
                    maxCounter = instructions[i].CounterIndex;
                }
            }
            int[] counters = new int[Math.Max(16, maxCounter + 1)];

            DateTime startTime = DateTime.UtcNow;
            double durationLimitMs = Math.Max(0.1, Macro.DurationSeconds) * 1000.0;

            try
            {
                using (PixelReader.CreateSession())
                {
                    bool continueOuterLoop = true;

                    while (continueOuterLoop)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (Macro.TriggerMode == MacroTriggerMode.Timed)
                        {
                            if ((DateTime.UtcNow - startTime).TotalMilliseconds >= durationLimitMs)
                            {
                                continueOuterLoop = false;
                                break;
                            }
                        }

                        IntPtr targetHwnd = IntPtr.Zero;
                        if (Macro.TargetMode == MacroTargetMode.ProTanki)
                        {
                            Process? gameProc = _getGameProcess();
                            if (gameProc == null || gameProc.HasExited)
                            {
                                LogService.LogWarning(
                                    $"Macro '{Macro.Name}' is set to target ProTanki, but ProTanki is not running."
                                );
                                continueOuterLoop = false;
                                break;
                            }

                            targetHwnd = gameProc.MainWindowHandle;
                        }

                        for (int ip = 0; ip < instructions.Length; ip++)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                continueOuterLoop = false;
                                break;
                            }

                            if (Macro.TriggerMode == MacroTriggerMode.Timed)
                            {
                                if (
                                    (DateTime.UtcNow - startTime).TotalMilliseconds
                                    >= durationLimitMs
                                )
                                {
                                    continueOuterLoop = false;
                                    break;
                                }
                            }

                            ref readonly Instruction inst = ref instructions[ip];

                            switch (inst.Opcode)
                            {
                                case Opcode.KeyPress:
                                    _inputDispatcher.SendKeyPress(
                                        inst.VirtualKey,
                                        inst.DurationMs,
                                        Macro.TargetMode,
                                        targetHwnd,
                                        cancellationToken
                                    );
                                    break;

                                case Opcode.KeyDown:
                                    _inputDispatcher.SendKeyDown(
                                        inst.VirtualKey,
                                        Macro.TargetMode,
                                        targetHwnd
                                    );
                                    break;

                                case Opcode.KeyUp:
                                    _inputDispatcher.SendKeyUp(
                                        inst.VirtualKey,
                                        Macro.TargetMode,
                                        targetHwnd
                                    );
                                    break;

                                case Opcode.MouseClick:
                                    int? clickX = inst.X >= 0 ? inst.X : null;
                                    int? clickY = inst.Y >= 0 ? inst.Y : null;
                                    _inputDispatcher.SendMouseClick(
                                        inst.MouseButton,
                                        clickX,
                                        clickY,
                                        inst.DurationMs,
                                        Macro.TargetMode,
                                        targetHwnd,
                                        cancellationToken
                                    );
                                    break;

                                case Opcode.MouseDown:
                                    int? downX = inst.X >= 0 ? inst.X : null;
                                    int? downY = inst.Y >= 0 ? inst.Y : null;
                                    _inputDispatcher.SendMouseDown(
                                        inst.MouseButton,
                                        downX,
                                        downY,
                                        Macro.TargetMode,
                                        targetHwnd
                                    );
                                    break;

                                case Opcode.MouseUp:
                                    int? upX = inst.X >= 0 ? inst.X : null;
                                    int? upY = inst.Y >= 0 ? inst.Y : null;
                                    _inputDispatcher.SendMouseUp(
                                        inst.MouseButton,
                                        upX,
                                        upY,
                                        Macro.TargetMode,
                                        targetHwnd
                                    );
                                    break;

                                case Opcode.MouseMove:
                                    _inputDispatcher.SendMouseMove(
                                        inst.X,
                                        inst.Y,
                                        Macro.TargetMode,
                                        targetHwnd
                                    );
                                    break;

                                case Opcode.Delay:
                                    ExecuteDelay(
                                        inst.DurationMs,
                                        Macro.Humanize,
                                        startTime,
                                        durationLimitMs,
                                        cancellationToken
                                    );
                                    break;

                                case Opcode.DelayRange:
                                    double rangeMs =
                                        inst.DurationMs
                                        + (
                                            Random.Shared.NextDouble()
                                            * (inst.MaxDurationMs - inst.DurationMs)
                                        );
                                    ExecuteDelay(
                                        rangeMs,
                                        false,
                                        startTime,
                                        durationLimitMs,
                                        cancellationToken
                                    );
                                    break;

                                case Opcode.CheckPixelMatch:
                                    uint actualColorMatch = PixelReader.GetScreenPixelColor(
                                        inst.X,
                                        inst.Y
                                    );
                                    if (
                                        !PixelReader.ColorsMatch(
                                            actualColorMatch,
                                            inst.ExpectedColor,
                                            18
                                        )
                                    )
                                    {
                                        if (
                                            inst.JumpTarget < 0
                                            || inst.JumpTarget > instructions.Length
                                        )
                                        {
                                            LogService.LogError(
                                                $"Invalid jump target {inst.JumpTarget} at IP {ip} in macro '{Macro.Name}'. Total instructions: {instructions.Length}"
                                            );
                                            continueOuterLoop = false;
                                            ip = instructions.Length;
                                            break;
                                        }
                                        if (inst.JumpTarget == instructions.Length)
                                        {
                                            ip = instructions.Length;
                                            break;
                                        }
                                        ip = inst.JumpTarget - 1;
                                    }
                                    break;

                                case Opcode.CheckPixelMismatch:
                                    uint actualColorMismatch = PixelReader.GetScreenPixelColor(
                                        inst.X,
                                        inst.Y
                                    );
                                    if (
                                        PixelReader.ColorsMatch(
                                            actualColorMismatch,
                                            inst.ExpectedColor,
                                            18
                                        )
                                    )
                                    {
                                        if (
                                            inst.JumpTarget < 0
                                            || inst.JumpTarget > instructions.Length
                                        )
                                        {
                                            LogService.LogError(
                                                $"Invalid jump target {inst.JumpTarget} at IP {ip} in macro '{Macro.Name}'. Total instructions: {instructions.Length}"
                                            );
                                            continueOuterLoop = false;
                                            ip = instructions.Length;
                                            break;
                                        }
                                        if (inst.JumpTarget == instructions.Length)
                                        {
                                            ip = instructions.Length;
                                            break;
                                        }
                                        ip = inst.JumpTarget - 1;
                                    }
                                    break;

                                case Opcode.Jump:
                                    if (
                                        inst.JumpTarget < 0
                                        || inst.JumpTarget > instructions.Length
                                    )
                                    {
                                        LogService.LogError(
                                            $"Invalid jump target {inst.JumpTarget} at IP {ip} in macro '{Macro.Name}'. Total instructions: {instructions.Length}"
                                        );
                                        continueOuterLoop = false;
                                        ip = instructions.Length;
                                        break;
                                    }
                                    if (inst.JumpTarget == instructions.Length)
                                    {
                                        ip = instructions.Length;
                                        break;
                                    }
                                    ip = inst.JumpTarget - 1;
                                    break;

                                case Opcode.RepeatStart:
                                    if (inst.CounterIndex < counters.Length)
                                    {
                                        counters[inst.CounterIndex] = 0;
                                    }
                                    break;

                                case Opcode.RepeatEnd:
                                    if (inst.CounterIndex < counters.Length)
                                    {
                                        if (
                                            inst.JumpTarget < 0
                                            || inst.JumpTarget >= instructions.Length
                                        )
                                        {
                                            LogService.LogError(
                                                $"Invalid repeat jump target {inst.JumpTarget} at IP {ip} in macro '{Macro.Name}'. Total instructions: {instructions.Length}"
                                            );
                                            continueOuterLoop = false;
                                            ip = instructions.Length;
                                            break;
                                        }

                                        counters[inst.CounterIndex]++;
                                        ref readonly Instruction startInst = ref instructions[
                                            inst.JumpTarget
                                        ];
                                        if (counters[inst.CounterIndex] < startInst.RepeatCount)
                                        {
                                            ip = inst.JumpTarget;
                                        }
                                        else
                                        {
                                            counters[inst.CounterIndex] = 0;
                                        }
                                    }
                                    break;

                                case Opcode.Stop:
                                    continueOuterLoop = false;
                                    ip = instructions.Length;
                                    break;
                            }
                        }

                        if (Macro.TriggerMode == MacroTriggerMode.Once)
                        {
                            continueOuterLoop = false;
                        }
                        else if (Macro.TriggerMode == MacroTriggerMode.Timed)
                        {
                            if ((DateTime.UtcNow - startTime).TotalMilliseconds >= durationLimitMs)
                            {
                                continueOuterLoop = false;
                            }
                        }
                        else
                        {
                            if (
                                instructions.All(i =>
                                    i.Opcode is not Opcode.Delay and not Opcode.DelayRange
                                )
                            )
                            {
                                HighPrecisionTimer.Delay(5, cancellationToken);
                            }
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            Macro.RunCount++;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogService.LogInfo($"Macro '{Macro.Name}' execution was canceled.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogService.LogError($"Error in execution of macro '{Macro.Name}'.", ex);
                SetState(MacroExecutionState.Error);
            }
            finally
            {
                _inputDispatcher.ReleaseAllTrackedInputs();
                HighPrecisionTimer.DisableHighResolution();

                if (Macro.State != MacroExecutionState.Error)
                {
                    SetState(MacroExecutionState.Idle);
                }
            }
        }

        private void ExecuteDelay(
            double durationMs,
            bool humanize,
            DateTime startTime,
            double durationLimitMs,
            CancellationToken ct
        )
        {
            if (humanize && durationMs > 20)
            {
                double jitter = durationMs * 0.10;
                durationMs += (Random.Shared.NextDouble() * 2 * jitter) - jitter;
            }

            if (Macro.TriggerMode == MacroTriggerMode.Timed)
            {
                double remaining = durationMs;
                while (remaining > 0 && !ct.IsCancellationRequested)
                {
                    if ((DateTime.UtcNow - startTime).TotalMilliseconds >= durationLimitMs)
                    {
                        break;
                    }
                    double step = Math.Min(20.0, remaining);
                    HighPrecisionTimer.Delay(step, ct);
                    remaining -= step;
                }
            }
            else
            {
                HighPrecisionTimer.Delay(durationMs, ct);
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
