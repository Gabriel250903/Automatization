using System.Text;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Automatization.Hotkeys
{
    [JsonConverter(typeof(HotKeyConverter))]
    public class HotKey : IEquatable<HotKey>
    {
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }

        [JsonIgnore]
        public int VirtualKey { get; private set; }

        public HotKey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
            VirtualKey = KeyInterop.VirtualKeyFromKey(key);
        }

        public HotKey()
        {
            Key = Key.None;
            Modifiers = ModifierKeys.None;
            VirtualKey = 0;
        }

        public bool IsEmpty => Key == Key.None;

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

            _ = sb.Append(Key);
            return sb.ToString();
        }

        public bool Equals(HotKey? other)
        {
            return other is not null && (ReferenceEquals(this, other) || (Key == other.Key && Modifiers == other.Modifiers));
        }

        public override bool Equals(object? obj)
        {
            return obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((HotKey)obj)));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Key, (int)Modifiers);
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
