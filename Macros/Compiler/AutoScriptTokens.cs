using Automatization.Hotkeys;
using Automatization.Macros.Core;
using Automatization.Macros.Models;

namespace Automatization.Macros.Compiler
{
    public enum TokenType
    {
        EndOfFile = 0,
        Identifier = 1,
        StringLiteral = 2,
        NumberLiteral = 3,
        DurationMs = 4,
        HexColor = 5,
        Variable = 6,
        Macro = 10,
        Hotkey = 11,
        Mode = 12,
        Target = 13,
        Humanize = 14,
        Main = 15,
        Press = 16,
        Hold = 17,
        KeyDown = 18,
        KeyUp = 19,
        Sleep = 20,
        Wait = 21,
        Click = 22,
        DoubleClick = 23,
        MouseDown = 24,
        MouseUp = 25,
        Move = 26,
        To = 27,
        At = 28,
        For = 29,
        Left = 30,
        Right = 31,
        Middle = 32,
        If = 33,
        Else = 34,
        Pixel = 35,
        Repeat = 36,
        While = 37,
        Stop = 38,
        Var = 39,
        Colon = 50,
        Comma = 51,
        OpenParen = 52,
        CloseParen = 53,
        EqualsEquals = 54,
        NotEquals = 55,
        Tilde = 56,
        Assign = 57,
        NewLine = 58,
        Semicolon = 59,
    }

    public readonly struct Token(TokenType type, string value, int line, int column)
    {
        public readonly TokenType Type = type;
        public readonly string Value = value;
        public readonly int Line = line;
        public readonly int Column = column;

        public override string ToString()
        {
            return $"{Type}('{Value}') at Line {Line}:{Column}";
        }
    }

    public readonly record struct CompileDiagnostic(
        string Message,
        int Line,
        int Column,
        bool IsError = true
    );

    public class CompilationResult
    {
        public bool Success => Diagnostics.All(d => !d.IsError);
        public List<CompileDiagnostic> Diagnostics { get; } = [];
        public Instruction[] Instructions { get; set; } = [];
        public string? ParsedName { get; set; }
        public HotKey? ParsedHotKey { get; set; }
        public MacroTriggerMode? ParsedTriggerMode { get; set; }
        public MacroTargetMode? ParsedTargetMode { get; set; }
        public bool? ParsedHumanize { get; set; }
        public bool HasMetadata =>
            !string.IsNullOrEmpty(ParsedName)
            || ParsedHotKey != null
            || ParsedTriggerMode != null
            || ParsedTargetMode != null;

        public static CompilationResult Failed(params CompileDiagnostic[] diagnostics)
        {
            CompilationResult result = new();
            result.Diagnostics.AddRange(diagnostics);
            return result;
        }

        public static CompilationResult Succeeded(Instruction[] instructions)
        {
            return new CompilationResult { Instructions = instructions };
        }
    }
}
