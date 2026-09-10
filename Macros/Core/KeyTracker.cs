using System.Collections.Concurrent;

namespace Automatization.Macros.Core
{
    public readonly record struct TrackedKey(
        ushort VirtualKey,
        uint ScanCode,
        IntPtr TargetHwnd,
        bool IsExtended,
        MacroTargetMode TargetMode = MacroTargetMode.Global
    );

    public readonly record struct TrackedMouseButton(
        MacroMouseButton Button,
        IntPtr TargetHwnd,
        MacroTargetMode TargetMode = MacroTargetMode.Global
    );

    public class KeyTracker
    {
        private readonly ConcurrentDictionary<ushort, TrackedKey> _downKeys = new();
        private readonly ConcurrentDictionary<
            MacroMouseButton,
            TrackedMouseButton
        > _downMouseButtons = new();

        public void RecordKeyDown(
            ushort virtualKey,
            uint scanCode,
            IntPtr targetHwnd,
            bool isExtended = false,
            MacroTargetMode targetMode = MacroTargetMode.Global
        )
        {
            _downKeys[virtualKey] = new TrackedKey(
                virtualKey,
                scanCode,
                targetHwnd,
                isExtended,
                targetMode
            );
        }

        public void RecordKeyUp(ushort virtualKey)
        {
            _ = _downKeys.TryRemove(virtualKey, out _);
        }

        public void RecordMouseDown(
            MacroMouseButton button,
            IntPtr targetHwnd,
            MacroTargetMode targetMode = MacroTargetMode.Global
        )
        {
            _downMouseButtons[button] = new TrackedMouseButton(button, targetHwnd, targetMode);
        }

        public void RecordMouseUp(MacroMouseButton button)
        {
            _ = _downMouseButtons.TryRemove(button, out _);
        }

        public bool HasDownInputs => !_downKeys.IsEmpty || !_downMouseButtons.IsEmpty;

        public void ReleaseAll(
            Action<TrackedKey> releaseKeyAction,
            Action<TrackedMouseButton> releaseMouseAction
        )
        {
            foreach (ushort vk in _downKeys.Keys.ToArray())
            {
                if (_downKeys.TryRemove(vk, out TrackedKey keyInfo))
                {
                    try
                    {
                        releaseKeyAction(keyInfo);
                    }
                    catch { }
                }
            }

            foreach (MacroMouseButton button in _downMouseButtons.Keys.ToArray())
            {
                if (_downMouseButtons.TryRemove(button, out TrackedMouseButton mouseInfo))
                {
                    try
                    {
                        releaseMouseAction(mouseInfo);
                    }
                    catch { }
                }
            }
        }
    }
}
