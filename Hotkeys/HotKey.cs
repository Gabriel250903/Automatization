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

        public HotKey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public HotKey()
        {
            Key = Key.None;
            Modifiers = ModifierKeys.None;
        }

        public bool IsEmpty => Key == Key.None;

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "None";
            }

            var sb = new StringBuilder();
            if (Modifiers.HasFlag(ModifierKeys.Control))
            {
                sb.Append("Ctrl + ");
            }
            if (Modifiers.HasFlag(ModifierKeys.Shift))
            {
                sb.Append("Shift + ");
            }
            if (Modifiers.HasFlag(ModifierKeys.Alt))
            {
                sb.Append("Alt + ");
            }
            if (Modifiers.HasFlag(ModifierKeys.Windows))
            {
                sb.Append("Win + ");
            }
            sb.Append(Key);
            return sb.ToString();
        }

        public bool Equals(HotKey? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Key == other.Key && Modifiers == other.Modifiers;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HotKey)obj);
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
