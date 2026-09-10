using System.Text;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Automatization.Hotkeys
{
    [JsonConverter(typeof(HotKeyConverter))]
    public sealed class HotKey : IEquatable<HotKey>
    {
        public Key Key { get; init; }
        public MouseButton? MouseButton { get; init; }
        public ModifierKeys Modifiers { get; init; }

        [JsonIgnore]
        public int VirtualKey => Key == Key.None ? 0 : KeyInterop.VirtualKeyFromKey(Key);

        public HotKey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public HotKey(MouseButton mouseButton, ModifierKeys modifiers)
        {
            Key = Key.None;
            MouseButton = mouseButton;
            Modifiers = modifiers;
        }

        public HotKey()
        {
            Key = Key.None;
            Modifiers = ModifierKeys.None;
        }

        public bool IsEmpty => Key == Key.None && MouseButton == null;

        public static bool TryParse(string? value, out HotKey result)
        {
            result = new HotKey();
            if (
                string.IsNullOrWhiteSpace(value)
                || value.Equals("None", StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }

            string[] parts = value.Split('+');
            Key parsedKey = Key.None;
            MouseButton? parsedMouseButton = null;
            ModifierKeys parsedModifiers = ModifierKeys.None;
            bool foundKeyOrMouse = false;

            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                if (
                    string.Equals(trimmed, "Ctrl", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(trimmed, "Control", StringComparison.OrdinalIgnoreCase)
                )
                {
                    parsedModifiers |= ModifierKeys.Control;
                }
                else if (string.Equals(trimmed, "Shift", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModifiers |= ModifierKeys.Shift;
                }
                else if (string.Equals(trimmed, "Alt", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModifiers |= ModifierKeys.Alt;
                }
                else if (
                    string.Equals(trimmed, "Win", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(trimmed, "Windows", StringComparison.OrdinalIgnoreCase)
                )
                {
                    parsedModifiers |= ModifierKeys.Windows;
                }
                else if (Enum.TryParse<MouseButton>(trimmed, true, out MouseButton mouseBtn))
                {
                    parsedMouseButton = mouseBtn;
                    foundKeyOrMouse = true;
                }
                else if (Enum.TryParse<Key>(trimmed, true, out Key key))
                {
                    parsedKey = key;
                    foundKeyOrMouse = true;
                }
                else
                {
                    return false;
                }
            }

            if (!foundKeyOrMouse && parsedModifiers == ModifierKeys.None)
            {
                return false;
            }

            result = parsedMouseButton.HasValue
                ? new HotKey(parsedMouseButton.Value, parsedModifiers)
                : new HotKey(parsedKey, parsedModifiers);
            return true;
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "None";
            }

            StringBuilder sb = new();
            if (Modifiers.HasFlag(ModifierKeys.Control))
            {
                _ = sb.Append("Ctrl + ");
            }

            if (Modifiers.HasFlag(ModifierKeys.Shift))
            {
                _ = sb.Append("Shift + ");
            }

            if (Modifiers.HasFlag(ModifierKeys.Alt))
            {
                _ = sb.Append("Alt + ");
            }

            if (Modifiers.HasFlag(ModifierKeys.Windows))
            {
                _ = sb.Append("Win + ");
            }

            _ = MouseButton != null ? sb.Append(MouseButton.Value.ToString()) : sb.Append(Key);
            return sb.ToString();
        }

        public bool Equals(HotKey? other)
        {
            return other is not null
                && (
                    ReferenceEquals(this, other)
                    || (
                        Key == other.Key
                        && MouseButton == other.MouseButton
                        && Modifiers == other.Modifiers
                    )
                );
        }

        public override bool Equals(object? obj)
        {
            return obj is not null
                && (
                    ReferenceEquals(this, obj)
                    || (obj.GetType() == GetType() && Equals((HotKey)obj))
                );
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Key, MouseButton, (int)Modifiers);
        }

        public static bool operator ==(HotKey? left, HotKey? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HotKey? left, HotKey? right)
        {
            return !Equals(left, right);
        }
    }
}
