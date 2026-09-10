namespace Automatization.Macros.Models
{
    public enum Opcode : byte
    {
        Nop = 0,
        KeyPress = 1,
        KeyDown = 2,
        KeyUp = 3,
        MouseClick = 4,
        MouseDown = 5,
        MouseUp = 6,
        MouseMove = 7,
        Delay = 8,
        DelayRange = 9,
        CheckPixelMatch = 10,
        CheckPixelMismatch = 11,
        Jump = 12,
        JumpIfFalse = 13,
        RepeatStart = 14,
        RepeatEnd = 15,
        Stop = 16,
    }
}
