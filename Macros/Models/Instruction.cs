using Automatization.Macros.Core;

namespace Automatization.Macros.Models
{
    public readonly struct Instruction(
        Opcode opcode,
        ushort virtualKey = 0,
        MacroMouseButton mouseButton = MacroMouseButton.Left,
        int x = -1,
        int y = -1,
        uint expectedColor = 0,
        double durationMs = 0,
        double maxDurationMs = 0,
        int jumpTarget = 0,
        int repeatCount = 0,
        int counterIndex = 0
    )
    {
        public readonly Opcode Opcode = opcode;
        public readonly ushort VirtualKey = virtualKey;
        public readonly MacroMouseButton MouseButton = mouseButton;
        public readonly int X = x;
        public readonly int Y = y;
        public readonly uint ExpectedColor = expectedColor;
        public readonly double DurationMs = durationMs;
        public readonly double MaxDurationMs = maxDurationMs;
        public readonly int JumpTarget = jumpTarget;
        public readonly int RepeatCount = repeatCount;
        public readonly int CounterIndex = counterIndex;

        public static Instruction CreateKeyPress(ushort vk, double holdMs = 20)
        {
            return new(Opcode.KeyPress, virtualKey: vk, durationMs: holdMs);
        }

        public static Instruction CreateKeyDown(ushort vk)
        {
            return new(Opcode.KeyDown, virtualKey: vk);
        }

        public static Instruction CreateKeyUp(ushort vk)
        {
            return new(Opcode.KeyUp, virtualKey: vk);
        }

        public static Instruction CreateMouseClick(
            MacroMouseButton button,
            int? x = null,
            int? y = null,
            double holdMs = 15
        )
        {
            return new(
                Opcode.MouseClick,
                mouseButton: button,
                x: x ?? -1,
                y: y ?? -1,
                durationMs: holdMs
            );
        }

        public static Instruction CreateMouseDown(
            MacroMouseButton button,
            int? x = null,
            int? y = null
        )
        {
            return new(Opcode.MouseDown, mouseButton: button, x: x ?? -1, y: y ?? -1);
        }

        public static Instruction CreateMouseUp(
            MacroMouseButton button,
            int? x = null,
            int? y = null
        )
        {
            return new(Opcode.MouseUp, mouseButton: button, x: x ?? -1, y: y ?? -1);
        }

        public static Instruction CreateMouseMove(int x, int y)
        {
            return new(Opcode.MouseMove, x: x, y: y);
        }

        public static Instruction CreateDelay(double ms)
        {
            return new(Opcode.Delay, durationMs: ms);
        }

        public static Instruction CreateDelayRange(double minMs, double maxMs)
        {
            return new(Opcode.DelayRange, durationMs: minMs, maxDurationMs: maxMs);
        }

        public static Instruction CreatePixelCheck(
            int x,
            int y,
            uint color,
            bool match,
            int jumpTargetIfMismatch
        )
        {
            return new(
                match ? Opcode.CheckPixelMatch : Opcode.CheckPixelMismatch,
                x: x,
                y: y,
                expectedColor: color,
                jumpTarget: jumpTargetIfMismatch
            );
        }

        public static Instruction CreateJump(int jumpTarget)
        {
            return new(Opcode.Jump, jumpTarget: jumpTarget);
        }

        public static Instruction CreateJumpIfFalse(int jumpTarget)
        {
            return new(Opcode.JumpIfFalse, jumpTarget: jumpTarget);
        }

        public static Instruction CreateRepeatStart(int count, int counterIndex)
        {
            return new(Opcode.RepeatStart, repeatCount: count, counterIndex: counterIndex);
        }

        public static Instruction CreateRepeatEnd(int loopStartIndex, int counterIndex)
        {
            return new(Opcode.RepeatEnd, jumpTarget: loopStartIndex, counterIndex: counterIndex);
        }

        public static Instruction CreateStop()
        {
            return new(Opcode.Stop);
        }
    }
}
