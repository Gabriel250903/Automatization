using System.Globalization;
using System.Text;

namespace Automatization.Macros.Compiler
{
    public class AutoScriptLexer(string source)
    {
        private readonly string _source = source ?? string.Empty;
        private int _pos;
        private int _line = 1;
        private int _col = 1;
        private readonly List<CompileDiagnostic> _diagnostics = [];

        public IReadOnlyList<CompileDiagnostic> Diagnostics => _diagnostics;

        public List<Token> Tokenize()
        {
            List<Token> tokens = [];

            while (_pos < _source.Length)
            {
                int startLine = _line;
                int startCol = _col;
                char c = _source[_pos];

                if (c == '#')
                {
                    bool canBeColor =
                        tokens.Count > 0
                        && tokens[^1].Type
                            is TokenType.Assign
                                or TokenType.EqualsEquals
                                or TokenType.NotEquals
                                or TokenType.OpenParen
                                or TokenType.Comma;

                    if (canBeColor)
                    {
                        int hexCount = 0;
                        while (
                            _pos + 1 + hexCount < _source.Length
                            && Uri.IsHexDigit(_source[_pos + 1 + hexCount])
                        )
                        {
                            hexCount++;
                        }
                        bool isFollowedByWordChar =
                            _pos + 1 + hexCount < _source.Length
                            && (
                                char.IsLetterOrDigit(_source[_pos + 1 + hexCount])
                                || _source[_pos + 1 + hexCount] == '_'
                            );

                        if (
                            (hexCount == 3 || hexCount == 6 || hexCount == 8)
                            && !isFollowedByWordChar
                        )
                        {
                            Advance();
                            string hexStr = ReadHexDigits();
                            tokens.Add(new Token(TokenType.HexColor, hexStr, startLine, startCol));
                            continue;
                        }
                    }

                    SkipComment();
                    continue;
                }

                if (c == '/' && Peek() == '/')
                {
                    SkipComment();
                    continue;
                }

                if (c is '\r' or '\n')
                {
                    int tokenLine = _line;
                    int tokenCol = _col;
                    HandleNewLine();

                    if (tokens.Count > 0 && tokens[^1].Type != TokenType.NewLine)
                    {
                        tokens.Add(new Token(TokenType.NewLine, "\\n", tokenLine, tokenCol));
                    }
                    continue;
                }

                if (c == '\t')
                {
                    const int tabWidth = 4;
                    _col += tabWidth - ((_col - 1) % tabWidth);
                    _pos++;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    Advance();
                    continue;
                }

                if (c == ';')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.Semicolon, ";", startLine, startCol));
                    continue;
                }

                if (c == '$')
                {
                    Advance();
                    string varName = ReadIdentifierName();
                    tokens.Add(new Token(TokenType.Variable, varName, startLine, startCol));
                    continue;
                }

                if (c is '"' or '\'')
                {
                    string strVal = ReadStringLiteral(c);
                    tokens.Add(new Token(TokenType.StringLiteral, strVal, startLine, startCol));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    tokens.Add(ReadNumberOrDuration(startLine, startCol));
                    continue;
                }

                if (c == '=')
                {
                    Advance();
                    if (_pos < _source.Length && _source[_pos] == '=')
                    {
                        Advance();
                        tokens.Add(new Token(TokenType.EqualsEquals, "==", startLine, startCol));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Assign, "=", startLine, startCol));
                    }
                    continue;
                }

                if (c == '!' && Peek() == '=')
                {
                    Advance();
                    Advance();
                    tokens.Add(new Token(TokenType.NotEquals, "!=", startLine, startCol));
                    continue;
                }

                if (c == ':')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.Colon, ":", startLine, startCol));
                    continue;
                }

                if (c == ',')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.Comma, ",", startLine, startCol));
                    continue;
                }

                if (c == '(')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.OpenParen, "(", startLine, startCol));
                    continue;
                }

                if (c == ')')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.CloseParen, ")", startLine, startCol));
                    continue;
                }

                if (c == '~')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.Tilde, "~", startLine, startCol));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    string ident = ReadIdentifierName();
                    TokenType kwType = MatchKeyword(ident);
                    tokens.Add(new Token(kwType, ident, startLine, startCol));
                    continue;
                }

                _diagnostics.Add(
                    new CompileDiagnostic($"Unexpected character '{c}'", startLine, startCol)
                );
                Advance();
            }

            tokens.Add(new Token(TokenType.EndOfFile, string.Empty, _line, _col));
            return tokens;
        }

        private char Peek()
        {
            return _pos + 1 < _source.Length ? _source[_pos + 1] : '\0';
        }

        private void Advance()
        {
            _pos++;
            _col++;
        }

        private void HandleNewLine()
        {
            if (_source[_pos] == '\r' && Peek() == '\n')
            {
                _pos += 2;
            }
            else
            {
                _pos++;
            }
            _line++;
            _col = 1;
        }

        private void SkipComment()
        {
            while (_pos < _source.Length && _source[_pos] != '\r' && _source[_pos] != '\n')
            {
                _pos++;
            }
        }

        private string ReadIdentifierName()
        {
            StringBuilder sb = new();
            while (
                _pos < _source.Length
                && (char.IsLetterOrDigit(_source[_pos]) || _source[_pos] == '_')
            )
            {
                _ = sb.Append(_source[_pos]);
                Advance();
            }
            return sb.ToString();
        }

        private string ReadHexDigits()
        {
            StringBuilder sb = new();
            while (_pos < _source.Length && Uri.IsHexDigit(_source[_pos]))
            {
                _ = sb.Append(_source[_pos]);
                Advance();
            }
            return sb.ToString();
        }

        private string ReadStringLiteral(char quote)
        {
            Advance();
            StringBuilder sb = new();
            while (_pos < _source.Length && _source[_pos] != quote)
            {
                if (_source[_pos] == '\\' && _pos + 1 < _source.Length)
                {
                    Advance();
                    char esc = _source[_pos];
                    _ = sb.Append(
                        esc switch
                        {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            _ => esc,
                        }
                    );
                }
                else
                {
                    _ = sb.Append(_source[_pos]);
                }
                Advance();
            }

            if (_pos < _source.Length && _source[_pos] == quote)
            {
                Advance();
            }
            else
            {
                _diagnostics.Add(new CompileDiagnostic("Unterminated string literal", _line, _col));
            }

            return sb.ToString();
        }

        private Token ReadNumberOrDuration(int line, int col)
        {
            if (_source[_pos] == '0' && (Peek() == 'x' || Peek() == 'X'))
            {
                Advance();
                Advance();
                string hexDigits = ReadHexDigits();
                return new Token(TokenType.HexColor, hexDigits, line, col);
            }

            StringBuilder sb = new();
            bool hasDot = false;

            while (
                _pos < _source.Length
                && (char.IsDigit(_source[_pos]) || (_source[_pos] == '.' && !hasDot))
            )
            {
                if (_source[_pos] == '.')
                {
                    hasDot = true;
                }
                _ = sb.Append(_source[_pos]);
                Advance();
            }

            string numStr = sb.ToString();
            _ = double.TryParse(
                numStr,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value
            );

            if (_pos < _source.Length && char.IsLetter(_source[_pos]))
            {
                string unit = ReadIdentifierName();
                string unitLower = unit.ToLowerInvariant();
                if (unitLower == "s")
                {
                    value *= 1000.0;
                    return new Token(
                        TokenType.DurationMs,
                        value.ToString(CultureInfo.InvariantCulture),
                        line,
                        col
                    );
                }
                if (unitLower == "ms")
                {
                    return new Token(
                        TokenType.DurationMs,
                        value.ToString(CultureInfo.InvariantCulture),
                        line,
                        col
                    );
                }

                _diagnostics.Add(
                    new CompileDiagnostic(
                        $"Invalid duration unit '{unit}' in '{numStr}{unit}'. Expected 'ms' or 's'.",
                        line,
                        col
                    )
                );
                return new Token(
                    TokenType.DurationMs,
                    value.ToString(CultureInfo.InvariantCulture),
                    line,
                    col
                );
            }

            return new Token(TokenType.NumberLiteral, numStr, line, col);
        }

        private static TokenType MatchKeyword(string ident)
        {
            return ident.ToLowerInvariant() switch
            {
                "macro" => TokenType.Macro,
                "hotkey" => TokenType.Hotkey,
                "mode" => TokenType.Mode,
                "target" => TokenType.Target,
                "humanize" => TokenType.Humanize,
                "main" => TokenType.Main,
                "press" => TokenType.Press,
                "hold" => TokenType.Hold,
                "key_down" or "keydown" => TokenType.KeyDown,
                "key_up" or "keyup" => TokenType.KeyUp,
                "sleep" => TokenType.Sleep,
                "wait" => TokenType.Wait,
                "click" => TokenType.Click,
                "double_click" or "doubleclick" => TokenType.DoubleClick,
                "mouse_down" or "mousedown" => TokenType.MouseDown,
                "mouse_up" or "mouseup" => TokenType.MouseUp,
                "move" => TokenType.Move,
                "to" => TokenType.To,
                "at" => TokenType.At,
                "for" => TokenType.For,
                "left" => TokenType.Left,
                "right" => TokenType.Right,
                "middle" => TokenType.Middle,
                "if" => TokenType.If,
                "else" => TokenType.Else,
                "pixel" => TokenType.Pixel,
                "repeat" => TokenType.Repeat,
                "while" => TokenType.While,
                "stop" => TokenType.Stop,
                "var" => TokenType.Var,
                _ => TokenType.Identifier,
            };
        }
    }
}
