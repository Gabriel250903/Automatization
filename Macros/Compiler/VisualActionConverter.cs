using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Automatization.Macros.Models;

namespace Automatization.Macros.Compiler
{
    public static class VisualActionConverter
    {
        public static string UpdateScriptMetadata(string? script, MacroDefinition macro)
        {
            if (macro == null)
            {
                return script ?? string.Empty;
            }

            string name = string.IsNullOrWhiteSpace(macro.Name) ? "New Macro" : macro.Name;
            string hotkey = macro.TriggerHotKey.IsEmpty ? "None" : macro.TriggerHotKey.ToString();

            if (string.IsNullOrWhiteSpace(script))
            {
                StringBuilder sb = new();
                _ = sb.AppendLine($"macro \"{name.Replace("\"", "\\\"")}\"");
                _ = sb.AppendLine($"hotkey: \"{hotkey}\"");
                _ = sb.AppendLine($"mode: {macro.TriggerMode}");
                _ = sb.AppendLine($"target: {macro.TargetMode}");
                _ = sb.AppendLine($"humanize: {macro.Humanize.ToString().ToLowerInvariant()}");
                _ = sb.AppendLine();
                _ = sb.AppendLine("main:");
                _ = sb.AppendLine("    sleep 50ms");
                return sb.ToString();
            }

            script = UpdateScriptDirective(
                script,
                "macro",
                $"macro \"{name.Replace("\"", "\\\"")}\""
            );
            script = UpdateScriptDirective(script, "hotkey", $"hotkey: \"{hotkey}\"");
            script = UpdateScriptDirective(script, "mode", $"mode: {macro.TriggerMode}");
            script = UpdateScriptDirective(script, "target", $"target: {macro.TargetMode}");
            script = UpdateScriptDirective(
                script,
                "humanize",
                $"humanize: {macro.Humanize.ToString().ToLowerInvariant()}"
            );

            return script;
        }

        public static string UpdateScriptDirective(
            string? script,
            string directivePrefix,
            string newDirectiveLine
        )
        {
            string eol = (script != null && script.Contains("\r\n")) ? "\r\n" : "\n";

            if (string.IsNullOrWhiteSpace(script))
            {
                return newDirectiveLine + eol;
            }

            List<string> lines = [.. script.Replace("\r\n", "\n").Split('\n')];

            string pattern = directivePrefix.Equals("macro", StringComparison.OrdinalIgnoreCase)
                ? @"^[ \t]*macro(\s+|:)"
                : $@"^[ \t]*{Regex.Escape(directivePrefix)}\s*:";

            List<int> matchingIndices = lines
                .Select((line, idx) => (line, idx))
                .Where(x => Regex.IsMatch(x.line, pattern, RegexOptions.IgnoreCase))
                .Select(x => x.idx)
                .ToList();

            if (matchingIndices.Count > 0)
            {
                int firstIdx = matchingIndices[0];
                lines[firstIdx] = newDirectiveLine;

                for (int i = matchingIndices.Count - 1; i > 0; i--)
                {
                    lines.RemoveAt(matchingIndices[i]);
                }
            }
            else
            {
                int insertAt = FindInsertIndexForDirective(lines, directivePrefix);
                lines.Insert(insertAt, newDirectiveLine);
            }

            return string.Join(eol, lines);
        }

        private static int FindInsertIndexForDirective(List<string> lines, string directivePrefix)
        {
            if (directivePrefix.Equals("macro", StringComparison.OrdinalIgnoreCase))
            {
                return FindTopInsertIndex(lines);
            }

            string[] order = ["macro", "hotkey", "mode", "target", "humanize"];
            int targetOrderIdx = Array.IndexOf(order, directivePrefix.ToLowerInvariant());

            for (int i = targetOrderIdx - 1; i >= 0; i--)
            {
                string prevPrefix = order[i];
                string prevPattern = prevPrefix.Equals("macro", StringComparison.OrdinalIgnoreCase)
                    ? @"^[ \t]*macro(\s+|:)"
                    : $@"^[ \t]*{Regex.Escape(prevPrefix)}\s*:";

                int foundIdx = lines.FindLastIndex(l =>
                    Regex.IsMatch(l, prevPattern, RegexOptions.IgnoreCase)
                );
                if (foundIdx >= 0)
                {
                    return foundIdx + 1;
                }
            }

            return FindTopInsertIndex(lines);
        }

        private static int FindTopInsertIndex(List<string> lines)
        {
            int index = 0;
            while (index < lines.Count)
            {
                string trimmed = lines[index].Trim();
                if (
                    trimmed.StartsWith('#')
                    || trimmed.StartsWith("//")
                    || string.IsNullOrEmpty(trimmed)
                )
                {
                    index++;
                }
                else
                {
                    break;
                }
            }
            return index;
        }

        public static string ToScript(
            IEnumerable<MacroAction> actions,
            MacroDefinition? macro = null
        )
        {
            StringBuilder sb = new();

            if (macro != null)
            {
                string name = string.IsNullOrWhiteSpace(macro.Name) ? "New Macro" : macro.Name;
                _ = sb.AppendLine($"macro \"{name.Replace("\"", "\\\"")}\"");
                string hotkey = macro.TriggerHotKey.IsEmpty
                    ? "None"
                    : macro.TriggerHotKey.ToString();
                _ = sb.AppendLine($"hotkey: \"{hotkey}\"");
                _ = sb.AppendLine($"mode: {macro.TriggerMode}");
                _ = sb.AppendLine($"target: {macro.TargetMode}");
                _ = sb.AppendLine($"humanize: {macro.Humanize.ToString().ToLowerInvariant()}");
                _ = sb.AppendLine();
            }

            _ = sb.AppendLine("main:");

            List<MacroAction> actionList = actions as List<MacroAction> ?? [.. actions];
            if (actionList.Count == 0)
            {
                _ = sb.AppendLine("    sleep 50ms");
            }
            else
            {
                foreach (MacroAction action in actionList)
                {
                    AppendAction(sb, action, "    ");
                }
            }

            return sb.ToString();
        }

        private static void AppendAction(StringBuilder sb, MacroAction action, string indent)
        {
            switch (action.Type)
            {
                case MacroActionType.KeyPress:
                    string keyName = FormatKeyName(action.Key);
                    _ = sb.AppendLine($"{indent}press \"{keyName}\" for {action.HoldMs:F0}ms");
                    break;

                case MacroActionType.KeyDown:
                    _ = sb.AppendLine($"{indent}key_down \"{FormatKeyName(action.Key)}\"");
                    break;

                case MacroActionType.KeyUp:
                    _ = sb.AppendLine($"{indent}key_up \"{FormatKeyName(action.Key)}\"");
                    break;

                case MacroActionType.Delay:
                    _ = sb.AppendLine($"{indent}sleep {action.DelayMs:F0}ms");
                    break;

                case MacroActionType.DelayRange:
                    _ = sb.AppendLine(
                        $"{indent}sleep {action.MinDelayMs:F0}ms ~ {action.MaxDelayMs:F0}ms"
                    );
                    break;

                case MacroActionType.MouseClick:
                    string btn = action.MouseButton.ToString().ToLowerInvariant();
                    _ = sb.AppendLine(
                        $"{indent}click {btn} at ({action.X}, {action.Y}) for {action.HoldMs:F0}ms"
                    );
                    break;

                case MacroActionType.MouseDown:
                    _ = sb.AppendLine(
                        $"{indent}mouse_down {action.MouseButton.ToString().ToLowerInvariant()} at ({action.X}, {action.Y})"
                    );
                    break;

                case MacroActionType.MouseUp:
                    _ = sb.AppendLine(
                        $"{indent}mouse_up {action.MouseButton.ToString().ToLowerInvariant()} at ({action.X}, {action.Y})"
                    );
                    break;

                case MacroActionType.MouseMove:
                    _ = sb.AppendLine($"{indent}move to ({action.X}, {action.Y})");
                    break;

                case MacroActionType.CheckPixel:
                    _ = sb.AppendLine(
                        $"{indent}if pixel({action.X}, {action.Y}) == {action.ColorHex}:"
                    );
                    if (action.Children.Count == 0)
                    {
                        _ = sb.AppendLine($"{indent}    sleep 50ms");
                    }
                    else
                    {
                        foreach (MacroAction child in action.Children)
                        {
                            AppendAction(sb, child, indent + "    ");
                        }
                    }
                    break;

                case MacroActionType.Repeat:
                    _ = sb.AppendLine($"{indent}repeat {action.RepeatCount}:");
                    if (action.Children.Count == 0)
                    {
                        _ = sb.AppendLine($"{indent}    sleep 50ms");
                    }
                    else
                    {
                        foreach (MacroAction child in action.Children)
                        {
                            AppendAction(sb, child, indent + "    ");
                        }
                    }
                    break;

                case MacroActionType.Stop:
                    _ = sb.AppendLine($"{indent}stop");
                    break;
            }
        }

        private static string FormatKeyName(Key key)
        {
            return key is >= Key.D0 and <= Key.D9 ? (key - Key.D0).ToString()
                : key is >= Key.NumPad0 and <= Key.NumPad9 ? $"NumPad{key - Key.NumPad0}"
                : key == Key.None ? "1"
                : key.ToString();
        }

        public static List<MacroAction> FromInstructions(IEnumerable<Instruction> instructions)
        {
            List<MacroAction> root = [];
            Stack<List<MacroAction>> containerStack = new();
            containerStack.Push(root);

            foreach (Instruction inst in instructions)
            {
                List<MacroAction> currentContainer = containerStack.Peek();

                switch (inst.Opcode)
                {
                    case Opcode.KeyPress:
                        currentContainer.Add(
                            new MacroAction
                            {
                                Type = MacroActionType.KeyPress,
                                Key = KeyNameHelper.VirtualKeyToKey(inst.VirtualKey),
                                HoldMs = inst.DurationMs,
                            }
                        );
                        break;

                    case Opcode.KeyDown:
                        currentContainer.Add(
                            new MacroAction
                            {
                                Type = MacroActionType.KeyDown,
                                Key = KeyNameHelper.VirtualKeyToKey(inst.VirtualKey),
                            }
                        );
                        break;

                    case Opcode.KeyUp:
                        currentContainer.Add(
                            new MacroAction
                            {
                                Type = MacroActionType.KeyUp,
                                Key = KeyNameHelper.VirtualKeyToKey(inst.VirtualKey),
                            }
                        );
                        break;

                    case Opcode.Delay:
                        currentContainer.Add(
                            new MacroAction
                            {
                                Type = MacroActionType.Delay,
                                DelayMs = inst.DurationMs,
                            }
                        );
                        break;

                    case Opcode.DelayRange:
                        currentContainer.Add(
                            new MacroAction
                            {
                                Type = MacroActionType.DelayRange,
                                MinDelayMs = inst.DurationMs,
                                MaxDelayMs = inst.MaxDurationMs,
                            }
                        );
                        break;

                    case Opcode.MouseClick:
                        currentContainer.Add(
                            new MacroAction
                            {
                                Type = MacroActionType.MouseClick,
                                MouseButton = inst.MouseButton,
                                X = inst.X,
                                Y = inst.Y,
                                HoldMs = inst.DurationMs,
                            }
                        );
                        break;

                    case Opcode.MouseDown:
                        currentContainer.Add(
                            new MacroAction
                            {
                                Type = MacroActionType.MouseDown,
                                MouseButton = inst.MouseButton,
                                X = inst.X,
                                Y = inst.Y,
                            }
                        );
                        break;

                    case Opcode.MouseUp:
                        currentContainer.Add(
                            new MacroAction
                            {
                                Type = MacroActionType.MouseUp,
                                MouseButton = inst.MouseButton,
                                X = inst.X,
                                Y = inst.Y,
                            }
                        );
                        break;

                    case Opcode.MouseMove:
                        currentContainer.Add(
                            new MacroAction
                            {
                                Type = MacroActionType.MouseMove,
                                X = inst.X,
                                Y = inst.Y,
                            }
                        );
                        break;

                    case Opcode.CheckPixelMatch:
                    case Opcode.CheckPixelMismatch:
                        currentContainer.Add(
                            new MacroAction
                            {
                                Type = MacroActionType.CheckPixel,
                                X = inst.X,
                                Y = inst.Y,
                                ColorHex = $"#{inst.ExpectedColor:X6}",
                            }
                        );
                        break;

                    case Opcode.RepeatStart:
                        MacroAction repeatAction = new()
                        {
                            Type = MacroActionType.Repeat,
                            RepeatCount = inst.RepeatCount,
                        };
                        currentContainer.Add(repeatAction);
                        containerStack.Push(repeatAction.Children);
                        break;

                    case Opcode.RepeatEnd:
                        if (containerStack.Count > 1)
                        {
                            _ = containerStack.Pop();
                        }
                        break;

                    case Opcode.Stop:
                        currentContainer.Add(new MacroAction { Type = MacroActionType.Stop });
                        break;
                }
            }

            return root;
        }
    }
}
