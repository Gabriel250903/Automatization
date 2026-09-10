namespace Automatization.Macros.Core
{
    public enum MacroTargetMode
    {
        ProTanki = 0,
        Global = 1,
        Background = ProTanki,
        Foreground = Global,
    }

    public enum MacroTriggerMode
    {
        Forever = 0,
        Timed = 1,
        Once = 2,
        Toggle = Forever,
        Hold = Timed,
        SinglePress = Once,
    }

    public enum MacroMouseButton
    {
        Left = 0,
        Right = 1,
        Middle = 2,
    }

    public enum MacroExecutionState
    {
        Idle = 0,
        Running = 1,
        Paused = 2,
        Error = 3,
    }
}
