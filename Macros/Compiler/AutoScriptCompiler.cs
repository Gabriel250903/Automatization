using System.Globalization;
using System.Text;
using Automatization.Hotkeys;
using Automatization.Macros.Core;
using Automatization.Macros.Models;

namespace Automatization.Macros.Compiler
{
    public class AutoScriptCompiler
    {
        private List<Token> _tokens = [];
        private int _index;
        private readonly List<CompileDiagnostic> _diagnostics = [];
        private readonly Dictionary<string, string> _variables = new(
            StringComparer.OrdinalIgnoreCase
        );
        private int _counterIndex = 0;
        private bool _hasMain = false;

        private string? _parsedName;
        private HotKey? _parsedHotKey;
        private MacroTriggerMode? _parsedTriggerMode;
        private MacroTargetMode? _parsedTargetMode;
        private bool? _parsedHumanize;

        private bool _hasMacroDirective = false;
        private bool _hasHotkeyDirective = false;
        private bool _hasModeDirective = false;
        private bool _hasTargetDirective = false;
        private bool _hasHumanizeDirective = false;

        public CompilationResult Compile(
            string scriptText,
            MacroDefinition? targetDefinition = null,
            bool requireMetadata = false
        )
        {
            _diagnostics.Clear();
            _variables.Clear();
            _counterIndex = 0;
            _hasMain = false;
            _parsedName = null;
            _parsedHotKey = null;
            _parsedTriggerMode = null;
            _parsedTargetMode = null;
            _parsedHumanize = null;
            _hasMacroDirective = false;
            _hasHotkeyDirective = false;
            _hasModeDirective = false;
            _hasTargetDirective = false;
            _hasHumanizeDirective = false;

            AutoScriptLexer lexer = new(scriptText);
            _tokens = lexer.Tokenize();
            _diagnostics.AddRange(lexer.Diagnostics);

            _index = 0;
            List<Instruction> instructions = [];

            while (!IsAtEnd())
            {
                SkipNewLines();
                if (IsAtEnd())
                {
                    break;
                }

                Token current = Peek();

                if (CheckMetadataDirective(targetDefinition))
                {
                    continue;
                }

                if (current.Type == TokenType.Var)
                {
                    ParseVarDeclaration();
                    continue;
                }

                if (current.Type == TokenType.Main)
                {
                    Token mainTok = Advance();
                    if (_hasMain)
                    {
                        _diagnostics.Add(
                            new CompileDiagnostic(
                                "Multiple 'main:' functions are not allowed.",
                                mainTok.Line,
                                mainTok.Column
                            )
                        );
                    }

                    if (!Match(TokenType.Colon))
                    {
                        _diagnostics.Add(
                            new CompileDiagnostic(
                                "Expected ':' after 'main' (e.g. 'main:')",
                                mainTok.Line,
                                mainTok.Column
                            )
                        );
                    }

                    _hasMain = true;
                    ParseBlock(
                        instructions,
                        parentIndent: mainTok.Column,
                        blockName: "main:",
                        headerLine: mainTok.Line
                    );
                    continue;
                }

                if (_hasMain)
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Unexpected statement '{current.Value}' outside 'main:'. All executable macro statements must be indented inside the 'main:' function.",
                            current.Line,
                            current.Column
                        )
                    );
                    SkipToNextLine();
                    continue;
                }

                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Missing 'main:' function. Every macro script must start with a 'main:' function.",
                        current.Line,
                        current.Column
                    )
                );
                _hasMain = true;
                ParseStatement(instructions);
            }

            if (!_hasMain)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Missing 'main:' function. Every macro script must start with a 'main:' function.",
                        1,
                        1
                    )
                );
            }

            if (requireMetadata)
            {
                if (!_hasMacroDirective)
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Missing mandatory 'macro \"Name\"' directive at the top of the script.",
                            1,
                            1
                        )
                    );
                }
                if (!_hasHotkeyDirective)
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Missing mandatory 'hotkey: \"<hotkey>\"' directive at the top of the script. (Use 'hotkey: \"None\"' if unassigned).",
                            1,
                            1
                        )
                    );
                }
                if (!_hasModeDirective)
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Missing mandatory 'mode: Forever|Timed|Once' directive at the top of the script.",
                            1,
                            1
                        )
                    );
                }
                if (!_hasTargetDirective)
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Missing mandatory 'target: ProTanki|Global' directive at the top of the script.",
                            1,
                            1
                        )
                    );
                }
                if (!_hasHumanizeDirective)
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Missing mandatory 'humanize: true|false' directive at the top of the script.",
                            1,
                            1
                        )
                    );
                }
            }

            CompilationResult result = _diagnostics.Any(d => d.IsError)
                ? CompilationResult.Failed([.. _diagnostics])
                : CompilationResult.Succeeded([.. instructions]);

            result.ParsedName = _parsedName;
            result.ParsedHotKey = _parsedHotKey;
            result.ParsedTriggerMode = _parsedTriggerMode;
            result.ParsedTargetMode = _parsedTargetMode;
            result.ParsedHumanize = _parsedHumanize;

            if (result.Diagnostics.Count == 0 && _diagnostics.Count > 0)
            {
                result.Diagnostics.AddRange(_diagnostics);
            }

            return result;
        }

        private bool CheckMetadataDirective(MacroDefinition? targetDefinition)
        {
            Token token = Peek();

            if (
                token.Type
                is TokenType.Macro
                    or TokenType.Hotkey
                    or TokenType.Mode
                    or TokenType.Target
                    or TokenType.Humanize
            )
            {
                if (_hasMain)
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Metadata directives (e.g. 'macro', 'mode', 'target', 'hotkey', 'humanize') must be placed at the top of the script before 'main:'.",
                            token.Line,
                            token.Column,
                            IsError: false
                        )
                    );
                }
            }

            if (token.Type == TokenType.Macro)
            {
                _ = Advance();
                _ = Match(TokenType.Colon);
                if (Match(TokenType.StringLiteral, out Token nameTok))
                {
                    if (string.IsNullOrWhiteSpace(nameTok.Value))
                    {
                        _diagnostics.Add(
                            new CompileDiagnostic(
                                "Macro name cannot be empty (e.g. macro \"Combat\").",
                                nameTok.Line,
                                nameTok.Column
                            )
                        );
                    }
                    else
                    {
                        _hasMacroDirective = true;
                        _parsedName = nameTok.Value;
                        if (targetDefinition != null)
                        {
                            targetDefinition.Name = nameTok.Value;
                        }
                    }
                }
                else
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Expected macro name in quotes (e.g. macro \"Name\")",
                            token.Line,
                            token.Column
                        )
                    );
                }
                SkipOptionalColonAndNewLine();
                return true;
            }

            if (token.Type == TokenType.Hotkey)
            {
                _ = Advance();
                _ = Match(TokenType.Colon);
                if (
                    !IsAtEnd()
                    && Peek().Type != TokenType.NewLine
                    && Peek().Type != TokenType.EndOfFile
                )
                {
                    Token hotkeyTok = Peek();
                    string hotkeyStr = ReadRestOfLine().Trim().Trim('"', '\'');
                    if (!string.IsNullOrWhiteSpace(hotkeyStr))
                    {
                        if (
                            HotKey.TryParse(hotkeyStr, out HotKey? parsedHotKey)
                            && parsedHotKey != null
                        )
                        {
                            _hasHotkeyDirective = true;
                            _parsedHotKey = parsedHotKey;
                            if (targetDefinition != null)
                            {
                                targetDefinition.TriggerHotKey = parsedHotKey;
                            }
                        }
                        else
                        {
                            _diagnostics.Add(
                                new CompileDiagnostic(
                                    $"Invalid hotkey '{hotkeyStr}'",
                                    hotkeyTok.Line,
                                    hotkeyTok.Column
                                )
                            );
                        }
                    }
                    else
                    {
                        _diagnostics.Add(
                            new CompileDiagnostic(
                                "Missing hotkey value after 'hotkey:' directive (e.g. hotkey: \"None\" or hotkey: \"F1\").",
                                token.Line,
                                token.Column
                            )
                        );
                    }
                }
                else
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Missing hotkey value after 'hotkey:' directive (e.g. hotkey: \"None\" or hotkey: \"F1\").",
                            token.Line,
                            token.Column
                        )
                    );
                }
                SkipNewLines();
                return true;
            }

            if (token.Type == TokenType.Mode)
            {
                _ = Advance();
                _ = Match(TokenType.Colon);
                if (
                    MatchIdentifierOrKeyword(out string modeStr)
                    && !string.IsNullOrWhiteSpace(modeStr)
                )
                {
                    MacroTriggerMode? mode = null;
                    if (
                        modeStr.Equals("Forever", StringComparison.OrdinalIgnoreCase)
                        || modeStr.Equals("Toggle", StringComparison.OrdinalIgnoreCase)
                        || modeStr.Equals("Infinite", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        mode = MacroTriggerMode.Forever;
                    }
                    else if (
                        modeStr.Equals("Timed", StringComparison.OrdinalIgnoreCase)
                        || modeStr.Equals("Duration", StringComparison.OrdinalIgnoreCase)
                        || modeStr.Equals("Hold", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        mode = MacroTriggerMode.Timed;
                    }
                    else if (
                        modeStr.Equals("Once", StringComparison.OrdinalIgnoreCase)
                        || modeStr.Equals("SinglePress", StringComparison.OrdinalIgnoreCase)
                        || modeStr.Equals("Single", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        mode = MacroTriggerMode.Once;
                    }
                    else if (Enum.TryParse(modeStr, true, out MacroTriggerMode triggerMode))
                    {
                        mode = triggerMode;
                    }
                    else
                    {
                        _diagnostics.Add(
                            new CompileDiagnostic(
                                $"Unknown trigger mode '{modeStr}'. Valid modes: 'Forever', 'Timed', 'Once'",
                                token.Line,
                                token.Column
                            )
                        );
                    }

                    if (mode.HasValue)
                    {
                        _hasModeDirective = true;
                        _parsedTriggerMode = mode.Value;
                        if (targetDefinition != null)
                        {
                            targetDefinition.TriggerMode = mode.Value;
                        }
                    }
                }
                else
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Missing trigger mode value after 'mode:' directive. Expected: Forever, Timed, or Once (e.g. mode: Forever)",
                            token.Line,
                            token.Column
                        )
                    );
                }
                SkipToNextLine();
                return true;
            }

            if (token.Type == TokenType.Target)
            {
                _ = Advance();
                _ = Match(TokenType.Colon);
                if (
                    MatchIdentifierOrKeyword(out string targetStr)
                    && !string.IsNullOrWhiteSpace(targetStr)
                )
                {
                    MacroTargetMode? target = null;
                    if (
                        targetStr.Equals("ProTanki", StringComparison.OrdinalIgnoreCase)
                        || targetStr.Equals("Background", StringComparison.OrdinalIgnoreCase)
                        || targetStr.Equals("Game", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        target = MacroTargetMode.ProTanki;
                    }
                    else if (
                        targetStr.Equals("Global", StringComparison.OrdinalIgnoreCase)
                        || targetStr.Equals("Foreground", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        target = MacroTargetMode.Global;
                    }
                    else if (Enum.TryParse(targetStr, true, out MacroTargetMode targetMode))
                    {
                        target = targetMode;
                    }
                    else
                    {
                        _diagnostics.Add(
                            new CompileDiagnostic(
                                $"Unknown target mode '{targetStr}'. Valid targets: 'ProTanki', 'Global'",
                                token.Line,
                                token.Column
                            )
                        );
                    }

                    if (target.HasValue)
                    {
                        _hasTargetDirective = true;
                        _parsedTargetMode = target.Value;
                        if (targetDefinition != null)
                        {
                            targetDefinition.TargetMode = target.Value;
                        }
                    }
                }
                else
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Missing target mode value after 'target:' directive. Expected: ProTanki or Global (e.g. target: ProTanki)",
                            token.Line,
                            token.Column
                        )
                    );
                }
                SkipToNextLine();
                return true;
            }

            if (token.Type == TokenType.Humanize)
            {
                _ = Advance();
                _ = Match(TokenType.Colon);
                if (
                    MatchIdentifierOrKeyword(out string humStr)
                    && !string.IsNullOrWhiteSpace(humStr)
                )
                {
                    if (bool.TryParse(humStr, out bool humVal))
                    {
                        _hasHumanizeDirective = true;
                        _parsedHumanize = humVal;
                        if (targetDefinition != null)
                        {
                            targetDefinition.Humanize = humVal;
                        }
                    }
                    else
                    {
                        _diagnostics.Add(
                            new CompileDiagnostic(
                                $"Invalid humanize value '{humStr}'. Expected 'true' or 'false'",
                                token.Line,
                                token.Column
                            )
                        );
                    }
                }
                else
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Missing value after 'humanize:' directive. Expected: true or false (e.g. humanize: true)",
                            token.Line,
                            token.Column
                        )
                    );
                }
                SkipToNextLine();
                return true;
            }

            return false;
        }

        private void ParseVarDeclaration()
        {
            _ = Advance();
            if (!Match(TokenType.Variable, out Token varToken))
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected variable name starting with '$'",
                        Peek().Line,
                        Peek().Column
                    )
                );
                SkipToNextLine();
                return;
            }

            if (!Match(TokenType.Assign))
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected '=' after variable name",
                        Peek().Line,
                        Peek().Column
                    )
                );
                SkipToNextLine();
                return;
            }

            if (IsAtEnd() || Peek().Type == TokenType.NewLine || Peek().Type == TokenType.EndOfFile)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected value after '=' in variable declaration",
                        Peek().Line,
                        Peek().Column
                    )
                );
                SkipToNextLine();
                return;
            }

            Token valToken = Advance();
            _variables[varToken.Value] = valToken.Value;
            SkipOptionalColonAndNewLine();
        }

        private void ParseBlock(
            List<Instruction> instructions,
            int parentIndent,
            string blockName,
            int headerLine
        )
        {
            if (
                !IsAtEnd()
                && Peek().Type != TokenType.NewLine
                && Peek().Type != TokenType.EndOfFile
                && Peek().Line == headerLine
            )
            {
                Token invalidTok = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Unexpected token '{invalidTok.Value}' on the same line as '{blockName}'. Statements inside '{blockName}' must be on a new line and indented.",
                        invalidTok.Line,
                        invalidTok.Column
                    )
                );
                SkipToNextLine();
            }

            SkipNewLines();
            if (IsAtEnd() || Peek().Type == TokenType.EndOfFile)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected an indented block after '{blockName}'. Statements inside must be indented.",
                        Peek().Line,
                        Peek().Column
                    )
                );
                return;
            }

            Token firstTok = Peek();
            if (firstTok.Column <= parentIndent)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected an indented block after '{blockName}'. Statements inside must be indented.",
                        firstTok.Line,
                        firstTok.Column
                    )
                );
                return;
            }

            int blockIndent = firstTok.Column;
            int statementCount = 0;

            while (!IsAtEnd())
            {
                SkipNewLines();
                if (IsAtEnd())
                {
                    break;
                }

                Token current = Peek();
                if (current.Type == TokenType.EndOfFile)
                {
                    break;
                }

                if (current.Column < blockIndent)
                {
                    break;
                }

                if (current.Column > blockIndent)
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Inconsistent indentation. Expected column {blockIndent}, but found column {current.Column}.",
                            current.Line,
                            current.Column
                        )
                    );
                }

                statementCount++;
                ParseStatement(instructions);
            }

            if (statementCount == 0)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected an indented block after '{blockName}'.",
                        firstTok.Line,
                        firstTok.Column
                    )
                );
            }
        }

        private void ParseStatement(List<Instruction> instructions)
        {
            Token token = Peek();

            switch (token.Type)
            {
                case TokenType.Press:
                    ParsePress(instructions);
                    break;

                case TokenType.Hold:
                    ParseHold(instructions);
                    break;

                case TokenType.KeyDown:
                    ParseKeyDown(instructions);
                    break;

                case TokenType.KeyUp:
                    ParseKeyUp(instructions);
                    break;

                case TokenType.Sleep:
                case TokenType.Wait:
                    ParseSleep(instructions);
                    break;

                case TokenType.Click:
                    ParseClick(instructions, doubleClick: false);
                    break;

                case TokenType.DoubleClick:
                    ParseClick(instructions, doubleClick: true);
                    break;

                case TokenType.MouseDown:
                    ParseMouseDown(instructions);
                    break;

                case TokenType.MouseUp:
                    ParseMouseUp(instructions);
                    break;

                case TokenType.Move:
                    ParseMove(instructions);
                    break;

                case TokenType.If:
                    ParseIf(instructions);
                    break;

                case TokenType.Else:
                    Token elseTok = Advance();
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "'else' statement without matching 'if'",
                            elseTok.Line,
                            elseTok.Column
                        )
                    );
                    SkipToNextLine();
                    break;

                case TokenType.Repeat:
                    ParseRepeat(instructions);
                    break;

                case TokenType.While:
                    ParseWhile(instructions);
                    break;

                case TokenType.Stop:
                    _ = Advance();
                    instructions.Add(Instruction.CreateStop());
                    SkipOptionalColonAndNewLine();
                    break;

                case TokenType.Var:
                    ParseVarDeclaration();
                    break;

                default:
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Unexpected statement token '{token.Value}'",
                            token.Line,
                            token.Column
                        )
                    );
                    _ = Advance();
                    SkipToNextLine();
                    break;
            }
        }

        #region Statement Parsers

        private void ParsePress(List<Instruction> instructions)
        {
            Token pressTok = Advance();
            if (IsAtEnd() || Peek().Type == TokenType.NewLine || Peek().Type == TokenType.EndOfFile)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected key name after 'press' (e.g. press \"1\" or press space)",
                        pressTok.Line,
                        pressTok.Column
                    )
                );
                SkipOptionalColonAndNewLine();
                return;
            }

            Token keyTok = Peek();
            string keyStr = ResolveValueString();
            ushort vk = KeyNameHelper.ParseVirtualKey(keyStr);
            if (vk == 0 && !string.IsNullOrEmpty(keyStr))
            {
                _diagnostics.Add(
                    new CompileDiagnostic($"Unknown key '{keyStr}'", keyTok.Line, keyTok.Column)
                );
            }

            double holdMs = 25;
            if (Match(TokenType.For))
            {
                if (
                    IsAtEnd()
                    || Peek().Type == TokenType.NewLine
                    || Peek().Type == TokenType.EndOfFile
                )
                {
                    Token forTok = _tokens[_index - 1];
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Expected duration after 'for' (e.g. 'for 25ms')",
                            forTok.Line,
                            forTok.Column
                        )
                    );
                }
                else
                {
                    holdMs = ParseDuration();
                }
            }

            if (vk != 0)
            {
                instructions.Add(Instruction.CreateKeyPress(vk, holdMs));
            }
            SkipOptionalColonAndNewLine();
        }

        private void ParseHold(List<Instruction> instructions)
        {
            Token holdTok = Advance();
            if (IsAtEnd() || Peek().Type == TokenType.NewLine || Peek().Type == TokenType.EndOfFile)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected key name after 'hold' (e.g. hold \"1\" or hold space)",
                        holdTok.Line,
                        holdTok.Column
                    )
                );
                SkipOptionalColonAndNewLine();
                return;
            }

            Token keyTok = Peek();
            string keyStr = ResolveValueString();
            ushort vk = KeyNameHelper.ParseVirtualKey(keyStr);
            if (vk == 0 && !string.IsNullOrEmpty(keyStr))
            {
                _diagnostics.Add(
                    new CompileDiagnostic($"Unknown key '{keyStr}'", keyTok.Line, keyTok.Column)
                );
            }

            double holdMs = 100;
            if (Match(TokenType.For))
            {
                if (
                    IsAtEnd()
                    || Peek().Type == TokenType.NewLine
                    || Peek().Type == TokenType.EndOfFile
                )
                {
                    Token forTok = _tokens[_index - 1];
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Expected duration after 'for' (e.g. 'for 100ms')",
                            forTok.Line,
                            forTok.Column
                        )
                    );
                }
                else
                {
                    holdMs = ParseDuration();
                }
            }

            if (vk != 0)
            {
                instructions.Add(Instruction.CreateKeyPress(vk, holdMs));
            }
            SkipOptionalColonAndNewLine();
        }

        private void ParseKeyDown(List<Instruction> instructions)
        {
            Token kdTok = Advance();
            if (IsAtEnd() || Peek().Type == TokenType.NewLine || Peek().Type == TokenType.EndOfFile)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected key name after 'key_down' (e.g. key_down \"W\")",
                        kdTok.Line,
                        kdTok.Column
                    )
                );
                SkipOptionalColonAndNewLine();
                return;
            }

            Token keyTok = Peek();
            string keyStr = ResolveValueString();
            ushort vk = KeyNameHelper.ParseVirtualKey(keyStr);
            if (vk == 0 && !string.IsNullOrEmpty(keyStr))
            {
                _diagnostics.Add(
                    new CompileDiagnostic($"Unknown key '{keyStr}'", keyTok.Line, keyTok.Column)
                );
            }
            else if (vk != 0)
            {
                instructions.Add(Instruction.CreateKeyDown(vk));
            }
            SkipOptionalColonAndNewLine();
        }

        private void ParseKeyUp(List<Instruction> instructions)
        {
            Token kuTok = Advance();
            if (IsAtEnd() || Peek().Type == TokenType.NewLine || Peek().Type == TokenType.EndOfFile)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected key name after 'key_up' (e.g. key_up \"W\")",
                        kuTok.Line,
                        kuTok.Column
                    )
                );
                SkipOptionalColonAndNewLine();
                return;
            }

            Token keyTok = Peek();
            string keyStr = ResolveValueString();
            ushort vk = KeyNameHelper.ParseVirtualKey(keyStr);
            if (vk == 0 && !string.IsNullOrEmpty(keyStr))
            {
                _diagnostics.Add(
                    new CompileDiagnostic($"Unknown key '{keyStr}'", keyTok.Line, keyTok.Column)
                );
            }
            else if (vk != 0)
            {
                instructions.Add(Instruction.CreateKeyUp(vk));
            }
            SkipOptionalColonAndNewLine();
        }

        private void ParseSleep(List<Instruction> instructions)
        {
            Token sleepTok = Advance();
            if (IsAtEnd() || Peek().Type == TokenType.NewLine || Peek().Type == TokenType.EndOfFile)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected duration after sleep (e.g. 'sleep 100ms' or 'sleep 100ms ~ 200ms')",
                        sleepTok.Line,
                        sleepTok.Column
                    )
                );
                SkipOptionalColonAndNewLine();
                return;
            }

            double minMs = ParseDuration();

            if (Match(TokenType.Tilde))
            {
                if (
                    IsAtEnd()
                    || Peek().Type == TokenType.NewLine
                    || Peek().Type == TokenType.EndOfFile
                )
                {
                    Token tildeTok = _tokens[_index - 1];
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Expected maximum duration after '~' (e.g. 'sleep 100ms ~ 200ms')",
                            tildeTok.Line,
                            tildeTok.Column
                        )
                    );
                }
                else
                {
                    Token maxTok = Peek();
                    double maxMs = ParseDuration();
                    if (maxMs < minMs)
                    {
                        _diagnostics.Add(
                            new CompileDiagnostic(
                                $"Maximum duration ({maxMs:F0}ms) must be greater than or equal to minimum duration ({minMs:F0}ms)",
                                maxTok.Line,
                                maxTok.Column
                            )
                        );
                    }
                    instructions.Add(Instruction.CreateDelayRange(minMs, maxMs));
                }
            }
            else
            {
                instructions.Add(Instruction.CreateDelay(minMs));
            }

            SkipOptionalColonAndNewLine();
        }

        private void ParseClick(List<Instruction> instructions, bool doubleClick)
        {
            _ = Advance();

            MacroMouseButton button = MacroMouseButton.Left;
            if (Match(TokenType.Left))
            {
                button = MacroMouseButton.Left;
            }
            else if (Match(TokenType.Right))
            {
                button = MacroMouseButton.Right;
            }
            else if (Match(TokenType.Middle))
            {
                button = MacroMouseButton.Middle;
            }
            else if (Match(TokenType.StringLiteral, out Token btnStrTok))
            {
                string btnLower = btnStrTok.Value.ToLowerInvariant();
                if (btnLower == "left")
                {
                    button = MacroMouseButton.Left;
                }
                else if (btnLower == "right")
                {
                    button = MacroMouseButton.Right;
                }
                else if (btnLower == "middle")
                {
                    button = MacroMouseButton.Middle;
                }
                else
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Unknown mouse button '{btnStrTok.Value}'. Expected 'left', 'right', or 'middle'.",
                            btnStrTok.Line,
                            btnStrTok.Column
                        )
                    );
                }
            }

            int? x = null;
            int? y = null;

            if (Match(TokenType.At))
            {
                (x, y) = ParseCoordinates(required: true);
                if (!x.HasValue || !y.HasValue)
                {
                    SkipToNextLine();
                    return;
                }
            }

            double holdMs = 20;
            if (Match(TokenType.For))
            {
                if (
                    IsAtEnd()
                    || Peek().Type == TokenType.NewLine
                    || Peek().Type == TokenType.EndOfFile
                )
                {
                    Token forTok = _tokens[_index - 1];
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            "Expected duration after 'for' (e.g. 'for 20ms')",
                            forTok.Line,
                            forTok.Column
                        )
                    );
                }
                else
                {
                    holdMs = ParseDuration();
                }
            }

            instructions.Add(Instruction.CreateMouseClick(button, x, y, holdMs));
            if (doubleClick)
            {
                instructions.Add(Instruction.CreateDelay(40));
                instructions.Add(Instruction.CreateMouseClick(button, x, y, holdMs));
            }

            SkipOptionalColonAndNewLine();
        }

        private void ParseMouseDown(List<Instruction> instructions)
        {
            _ = Advance();
            MacroMouseButton button = MacroMouseButton.Left;
            if (Match(TokenType.Left))
            {
                button = MacroMouseButton.Left;
            }
            else if (Match(TokenType.Right))
            {
                button = MacroMouseButton.Right;
            }
            else if (Match(TokenType.Middle))
            {
                button = MacroMouseButton.Middle;
            }
            else if (Match(TokenType.StringLiteral, out Token btnStrTok))
            {
                string btnLower = btnStrTok.Value.ToLowerInvariant();
                if (btnLower == "left")
                {
                    button = MacroMouseButton.Left;
                }
                else if (btnLower == "right")
                {
                    button = MacroMouseButton.Right;
                }
                else if (btnLower == "middle")
                {
                    button = MacroMouseButton.Middle;
                }
                else
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Unknown mouse button '{btnStrTok.Value}'. Expected 'left', 'right', or 'middle'.",
                            btnStrTok.Line,
                            btnStrTok.Column
                        )
                    );
                }
            }

            int? x = null;
            int? y = null;
            if (Match(TokenType.At))
            {
                (x, y) = ParseCoordinates(required: true);
                if (!x.HasValue || !y.HasValue)
                {
                    SkipToNextLine();
                    return;
                }
            }

            instructions.Add(Instruction.CreateMouseDown(button, x, y));
            SkipOptionalColonAndNewLine();
        }

        private void ParseMouseUp(List<Instruction> instructions)
        {
            _ = Advance();
            MacroMouseButton button = MacroMouseButton.Left;
            if (Match(TokenType.Left))
            {
                button = MacroMouseButton.Left;
            }
            else if (Match(TokenType.Right))
            {
                button = MacroMouseButton.Right;
            }
            else if (Match(TokenType.Middle))
            {
                button = MacroMouseButton.Middle;
            }
            else if (Match(TokenType.StringLiteral, out Token btnStrTok))
            {
                string btnLower = btnStrTok.Value.ToLowerInvariant();
                if (btnLower == "left")
                {
                    button = MacroMouseButton.Left;
                }
                else if (btnLower == "right")
                {
                    button = MacroMouseButton.Right;
                }
                else if (btnLower == "middle")
                {
                    button = MacroMouseButton.Middle;
                }
                else
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Unknown mouse button '{btnStrTok.Value}'. Expected 'left', 'right', or 'middle'.",
                            btnStrTok.Line,
                            btnStrTok.Column
                        )
                    );
                }
            }

            int? x = null;
            int? y = null;
            if (Match(TokenType.At))
            {
                (x, y) = ParseCoordinates(required: true);
                if (!x.HasValue || !y.HasValue)
                {
                    SkipToNextLine();
                    return;
                }
            }

            instructions.Add(Instruction.CreateMouseUp(button, x, y));
            SkipOptionalColonAndNewLine();
        }

        private void ParseMove(List<Instruction> instructions)
        {
            _ = Advance();
            if (!Match(TokenType.To))
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected 'to' after 'move' (e.g. 'move to (500, 300)'), but found '{t.Value}'",
                        t.Line,
                        t.Column
                    )
                );
                SkipToNextLine();
                return;
            }

            (int? x, int? y) = ParseCoordinates(required: true);
            if (x.HasValue && y.HasValue)
            {
                instructions.Add(Instruction.CreateMouseMove(x.Value, y.Value));
            }
            SkipOptionalColonAndNewLine();
        }

        private void ParseIf(List<Instruction> instructions)
        {
            Token ifTok = Advance();

            if (!Match(TokenType.Pixel))
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected 'pixel' condition after 'if' (e.g. 'if pixel(x, y) == #color:'), but found '{t.Value}'",
                        t.Line,
                        t.Column
                    )
                );
                SkipToNextLine();
                return;
            }

            (int? x, int? y) = ParseCoordinates(required: true);
            if (!x.HasValue || !y.HasValue)
            {
                SkipToNextLine();
                return;
            }

            bool expectMatch;
            if (Match(TokenType.EqualsEquals))
            {
                expectMatch = true;
            }
            else if (Match(TokenType.NotEquals))
            {
                expectMatch = false;
            }
            else
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected '==' or '!=' in pixel check, but found '{t.Value}'",
                        t.Line,
                        t.Column
                    )
                );
                SkipToNextLine();
                return;
            }

            uint? color = ParseColor();
            if (!color.HasValue)
            {
                SkipToNextLine();
                return;
            }

            if (!Match(TokenType.Colon))
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected ':' after pixel condition, but found '{t.Value}'",
                        t.Line,
                        t.Column
                    )
                );
            }
            SkipNewLines();

            int jumpCheckIndex = instructions.Count;
            instructions.Add(
                Instruction.CreatePixelCheck(x.Value, y.Value, color.Value, expectMatch, 0)
            );

            List<Instruction> ifBodyInstructions = [];
            ParseBlock(
                ifBodyInstructions,
                parentIndent: ifTok.Column,
                blockName: "if",
                headerLine: ifTok.Line
            );
            instructions.AddRange(ifBodyInstructions);

            SkipNewLines();
            if (!IsAtEnd() && Peek().Type == TokenType.Else && Peek().Column == ifTok.Column)
            {
                Token elseTok = Advance();
                if (!Match(TokenType.Colon))
                {
                    Token t = Peek();
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Expected ':' after 'else' (e.g. 'else:'), but found '{t.Value}'",
                            t.Line,
                            t.Column
                        )
                    );
                }
                SkipNewLines();

                int ifJumpOverElseIndex = instructions.Count;
                instructions.Add(Instruction.CreateJump(0));

                int elseStartIndex = instructions.Count;
                instructions[jumpCheckIndex] = Instruction.CreatePixelCheck(
                    x.Value,
                    y.Value,
                    color.Value,
                    expectMatch,
                    elseStartIndex
                );

                List<Instruction> elseBodyInstructions = [];
                ParseBlock(
                    elseBodyInstructions,
                    parentIndent: elseTok.Column,
                    blockName: "else",
                    headerLine: elseTok.Line
                );
                instructions.AddRange(elseBodyInstructions);

                int afterElseIndex = instructions.Count;
                instructions[ifJumpOverElseIndex] = Instruction.CreateJump(afterElseIndex);
            }
            else
            {
                int jumpTarget = instructions.Count;
                instructions[jumpCheckIndex] = Instruction.CreatePixelCheck(
                    x.Value,
                    y.Value,
                    color.Value,
                    expectMatch,
                    jumpTarget
                );
            }
        }

        private void ParseRepeat(List<Instruction> instructions)
        {
            Token repeatTok = Advance();

            if (IsAtEnd() || Peek().Type == TokenType.NewLine || Peek().Type == TokenType.EndOfFile)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected repeat count after 'repeat' (e.g. 'repeat 5:')",
                        repeatTok.Line,
                        repeatTok.Column
                    )
                );
                SkipOptionalColonAndNewLine();
                return;
            }

            Token countTok = Peek();
            string countStr = ResolveValueString();
            if (!int.TryParse(countStr, out int repeatCount) || repeatCount <= 0)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Invalid repeat count '{countStr}'. Expected positive integer.",
                        countTok.Line,
                        countTok.Column
                    )
                );
                repeatCount = 1;
            }

            if (!Match(TokenType.Colon))
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected ':' after repeat count, but found '{t.Value}'",
                        t.Line,
                        t.Column
                    )
                );
            }
            SkipNewLines();

            int counterId = _counterIndex++;
            int loopStartIndex = instructions.Count;
            instructions.Add(Instruction.CreateRepeatStart(repeatCount, counterId));

            List<Instruction> bodyInstructions = [];
            ParseBlock(
                bodyInstructions,
                parentIndent: repeatTok.Column,
                blockName: "repeat",
                headerLine: repeatTok.Line
            );
            instructions.AddRange(bodyInstructions);

            instructions.Add(Instruction.CreateRepeatEnd(loopStartIndex, counterId));
        }

        private void ParseWhile(List<Instruction> instructions)
        {
            Token whileTok = Advance();

            if (IsAtEnd() || Peek().Type == TokenType.NewLine || Peek().Type == TokenType.EndOfFile)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected condition after 'while' (e.g. 'while pixel(100, 200) == #FFFFFF:' or 'while true:')",
                        whileTok.Line,
                        whileTok.Column
                    )
                );
                SkipToNextLine();
                return;
            }

            int? x = null;
            int? y = null;
            uint? color = null;
            bool isWhileTrue = false;
            bool expectMatch = true;

            if (
                Match(TokenType.Identifier, out Token idTok)
                && string.Equals(idTok.Value, "true", StringComparison.OrdinalIgnoreCase)
            )
            {
                isWhileTrue = true;
            }
            else if (Match(TokenType.Pixel))
            {
                (x, y) = ParseCoordinates(required: true);
                if (!x.HasValue || !y.HasValue)
                {
                    SkipToNextLine();
                    return;
                }

                if (Match(TokenType.EqualsEquals))
                {
                    expectMatch = true;
                }
                else if (Match(TokenType.NotEquals))
                {
                    expectMatch = false;
                }
                else
                {
                    Token t = Peek();
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Expected '==' or '!=' in while pixel check, but found '{t.Value}'",
                            t.Line,
                            t.Column
                        )
                    );
                    SkipToNextLine();
                    return;
                }

                color = ParseColor();
                if (!color.HasValue)
                {
                    SkipToNextLine();
                    return;
                }
            }
            else
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected 'pixel' condition or 'true' after 'while', but found '{t.Value}'",
                        t.Line,
                        t.Column
                    )
                );
                SkipToNextLine();
                return;
            }

            if (!Match(TokenType.Colon))
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected ':' after while condition, but found '{t.Value}'",
                        t.Line,
                        t.Column
                    )
                );
            }
            SkipNewLines();

            int loopStartIndex = instructions.Count;
            int jumpCheckIndex = -1;

            if (!isWhileTrue)
            {
                jumpCheckIndex = instructions.Count;
                instructions.Add(
                    Instruction.CreatePixelCheck(x!.Value, y!.Value, color!.Value, expectMatch, 0)
                );
            }

            List<Instruction> bodyInstructions = [];
            ParseBlock(
                bodyInstructions,
                parentIndent: whileTok.Column,
                blockName: "while",
                headerLine: whileTok.Line
            );
            instructions.AddRange(bodyInstructions);

            instructions.Add(Instruction.CreateJump(loopStartIndex));

            if (!isWhileTrue)
            {
                int afterLoopIndex = instructions.Count;
                instructions[jumpCheckIndex] = Instruction.CreatePixelCheck(
                    x!.Value,
                    y!.Value,
                    color!.Value,
                    expectMatch,
                    afterLoopIndex
                );
            }
        }

        #endregion

        #region Helpers

        private (int?, int?) ParseCoordinates(bool required = true)
        {
            if (!Match(TokenType.OpenParen))
            {
                if (required)
                {
                    Token t = Peek();
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Expected '(' before coordinates, but found '{t.Value}'",
                            t.Line,
                            t.Column
                        )
                    );
                }
                return (null, null);
            }

            if (
                IsAtEnd()
                || Peek().Type == TokenType.NewLine
                || Peek().Type == TokenType.CloseParen
                || Peek().Type == TokenType.Comma
                || Peek().Type == TokenType.EndOfFile
            )
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected X coordinate inside parentheses (e.g. '(500, 300)')",
                        t.Line,
                        t.Column
                    )
                );
                if (Peek().Type == TokenType.CloseParen)
                {
                    _ = Advance();
                }

                return (null, null);
            }

            Token xTok = Peek();
            if (xTok.Type == TokenType.Variable && !_variables.ContainsKey(xTok.Value))
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Undefined variable '${xTok.Value}'",
                        xTok.Line,
                        xTok.Column
                    )
                );
                _ = Advance();
                SkipToCloseParen();
                return (null, null);
            }

            string xStr = ResolveValueString();
            if (
                !int.TryParse(
                    xStr,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int parsedX
                )
            )
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Invalid X coordinate '{xStr}'. Expected an integer.",
                        xTok.Line,
                        xTok.Column
                    )
                );
                SkipToCloseParen();
                return (null, null);
            }

            if (!Match(TokenType.Comma))
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected ',' between X and Y coordinates (e.g. '(500, 300)'), but found '{t.Value}'",
                        t.Line,
                        t.Column
                    )
                );
                SkipToCloseParen();
                return (null, null);
            }

            if (
                IsAtEnd()
                || Peek().Type == TokenType.NewLine
                || Peek().Type == TokenType.CloseParen
                || Peek().Type == TokenType.EndOfFile
            )
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected Y coordinate after comma (e.g. '(500, 300)')",
                        t.Line,
                        t.Column
                    )
                );
                _ = Match(TokenType.CloseParen);
                return (null, null);
            }

            Token yTok = Peek();
            if (yTok.Type == TokenType.Variable && !_variables.ContainsKey(yTok.Value))
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Undefined variable '${yTok.Value}'",
                        yTok.Line,
                        yTok.Column
                    )
                );
                _ = Advance();
                _ = Match(TokenType.CloseParen);
                return (null, null);
            }

            string yStr = ResolveValueString();
            if (
                !int.TryParse(
                    yStr,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int parsedY
                )
            )
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Invalid Y coordinate '{yStr}'. Expected an integer.",
                        yTok.Line,
                        yTok.Column
                    )
                );
                SkipToCloseParen();
                return (null, null);
            }

            if (!Match(TokenType.CloseParen))
            {
                Token t = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Expected ')' after Y coordinate, but found '{t.Value}'",
                        t.Line,
                        t.Column
                    )
                );
                return (null, null);
            }

            if (parsedX < 0 || parsedY < 0)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Coordinates must be non-negative: ({parsedX}, {parsedY})",
                        xTok.Line,
                        xTok.Column
                    )
                );
                return (null, null);
            }

            return (parsedX, parsedY);
        }

        private void SkipToCloseParen()
        {
            while (
                !IsAtEnd()
                && Peek().Type != TokenType.CloseParen
                && Peek().Type != TokenType.NewLine
                && Peek().Type != TokenType.EndOfFile
            )
            {
                _ = Advance();
            }
            _ = Match(TokenType.CloseParen);
        }

        private double ParseDuration()
        {
            Token token = Peek();
            if (token.Type is TokenType.DurationMs or TokenType.NumberLiteral)
            {
                _ = Advance();
                if (
                    double.TryParse(
                        token.Value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double val
                    )
                )
                {
                    if (token.Type == TokenType.NumberLiteral)
                    {
                        if (!IsAtEnd() && Peek().Type == TokenType.Identifier)
                        {
                            Token nextTok = Peek();
                            if (
                                string.Equals(
                                    nextTok.Value,
                                    "s",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                _ = Advance();
                                val *= 1000.0;
                            }
                            else if (
                                string.Equals(
                                    nextTok.Value,
                                    "ms",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                _ = Advance();
                            }
                            else
                            {
                                _diagnostics.Add(
                                    new CompileDiagnostic(
                                        $"Missing duration unit after '{token.Value}'. Expected 'ms' or 's' (e.g. '{token.Value}ms').",
                                        token.Line,
                                        token.Column
                                    )
                                );
                            }
                        }
                        else
                        {
                            _diagnostics.Add(
                                new CompileDiagnostic(
                                    $"Missing duration unit after '{token.Value}'. Expected 'ms' or 's' (e.g. '{token.Value}ms').",
                                    token.Line,
                                    token.Column
                                )
                            );
                        }
                    }

                    if (val <= 0)
                    {
                        _diagnostics.Add(
                            new CompileDiagnostic(
                                $"Duration must be greater than 0ms, but was {val:F0}ms",
                                token.Line,
                                token.Column
                            )
                        );
                        return 1;
                    }
                    return val;
                }
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Invalid duration value '{token.Value}'",
                        token.Line,
                        token.Column
                    )
                );
                return 50;
            }
            else if (token.Type == TokenType.Variable)
            {
                _ = Advance();
                if (!_variables.TryGetValue(token.Value, out string? varVal))
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Undefined variable '${token.Value}'",
                            token.Line,
                            token.Column
                        )
                    );
                    return 50;
                }

                if (
                    double.TryParse(
                        varVal,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double val
                    )
                )
                {
                    if (val <= 0)
                    {
                        _diagnostics.Add(
                            new CompileDiagnostic(
                                $"Duration variable '${token.Value}' must be greater than 0ms",
                                token.Line,
                                token.Column
                            )
                        );
                        return 1;
                    }
                    return val;
                }

                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Variable '${token.Value}' value '{varVal}' is not a valid duration",
                        token.Line,
                        token.Column
                    )
                );
                return 50;
            }

            _diagnostics.Add(
                new CompileDiagnostic(
                    $"Expected duration (e.g. '50ms' or '1.5s'), but found '{token.Value}'",
                    token.Line,
                    token.Column
                )
            );
            _ = Advance();
            return 50;
        }

        private uint? ParseColor()
        {
            if (IsAtEnd() || Peek().Type == TokenType.NewLine || Peek().Type == TokenType.EndOfFile)
            {
                _diagnostics.Add(
                    new CompileDiagnostic(
                        "Expected hex color (e.g. '#00FF00' or '#FFFFFF')",
                        Peek().Line,
                        Peek().Column
                    )
                );
                return null;
            }

            Token token = Peek();
            if (token.Type == TokenType.Variable)
            {
                _ = Advance();
                if (!_variables.TryGetValue(token.Value, out string? varVal))
                {
                    _diagnostics.Add(
                        new CompileDiagnostic(
                            $"Undefined variable '${token.Value}'",
                            token.Line,
                            token.Column
                        )
                    );
                    return null;
                }

                string hex = varVal.Trim().Trim('"', '\'').TrimStart('#');
                if (
                    uint.TryParse(
                        hex,
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out uint color
                    )
                )
                {
                    return color;
                }

                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Variable '${token.Value}' value '{varVal}' is not a valid hex color (e.g. '#00FF00')",
                        token.Line,
                        token.Column
                    )
                );
                return null;
            }

            if (token.Type is TokenType.HexColor or TokenType.StringLiteral)
            {
                _ = Advance();
                string hex = token.Value.TrimStart('#');
                if (
                    uint.TryParse(
                        hex,
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out uint color
                    )
                )
                {
                    return color;
                }
            }

            _diagnostics.Add(
                new CompileDiagnostic(
                    $"Expected hex color (e.g. '#00FF00' or '#FFFFFF'), but found '{token.Value}'",
                    token.Line,
                    token.Column
                )
            );
            _ = Advance();
            return null;
        }

        private string ResolveValueString()
        {
            Token tok = Advance();
            if (tok.Type == TokenType.Variable)
            {
                if (_variables.TryGetValue(tok.Value, out string? val))
                {
                    return val;
                }
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Undefined variable '${tok.Value}'",
                        tok.Line,
                        tok.Column
                    )
                );
                return string.Empty;
            }
            return tok.Value;
        }

        private string ReadRestOfLine()
        {
            StringBuilder sb = new();
            while (
                !IsAtEnd() && Peek().Type != TokenType.NewLine && Peek().Type != TokenType.EndOfFile
            )
            {
                Token t = Advance();
                if (sb.Length > 0)
                {
                    _ = sb.Append(' ');
                }
                _ = sb.Append(t.Value);
            }
            return sb.ToString();
        }

        private bool Match(TokenType type)
        {
            if (!IsAtEnd() && Peek().Type == type)
            {
                _ = Advance();
                return true;
            }
            return false;
        }

        private bool Match(TokenType type, out Token token)
        {
            if (!IsAtEnd() && Peek().Type == type)
            {
                token = Advance();
                return true;
            }
            token = default;
            return false;
        }

        private bool MatchIdentifierOrKeyword(out string val)
        {
            if (
                !IsAtEnd()
                && Peek().Type != TokenType.NewLine
                && Peek().Type != TokenType.EndOfFile
            )
            {
                Token t = Advance();
                val = t.Value;
                return true;
            }
            val = string.Empty;
            return false;
        }

        private Token Peek()
        {
            return _tokens[_index];
        }

        private Token Advance()
        {
            if (!IsAtEnd())
            {
                _index++;
            }
            return _tokens[_index - 1];
        }

        private bool IsAtEnd()
        {
            return _index >= _tokens.Count || _tokens[_index].Type == TokenType.EndOfFile;
        }

        private void SkipNewLines()
        {
            while (
                !IsAtEnd()
                && (Peek().Type == TokenType.NewLine || Peek().Type == TokenType.Semicolon)
            )
            {
                _ = Advance();
            }
        }

        private void SkipOptionalColonAndNewLine()
        {
            _ = Match(TokenType.Colon);
            _ = Match(TokenType.Semicolon);
            if (
                !IsAtEnd()
                && Peek().Type != TokenType.NewLine
                && Peek().Type != TokenType.Semicolon
                && Peek().Type != TokenType.EndOfFile
            )
            {
                Token junk = Peek();
                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Unexpected token '{junk.Value}' at end of statement",
                        junk.Line,
                        junk.Column
                    )
                );
                SkipToNextLine();
                return;
            }
            SkipNewLines();
        }

        private void SkipToNextLine()
        {
            while (
                !IsAtEnd()
                && Peek().Type != TokenType.NewLine
                && Peek().Type != TokenType.Semicolon
                && Peek().Type != TokenType.EndOfFile
            )
            {
                _ = Advance();
            }
            SkipNewLines();
        }

        #endregion
    }
}
