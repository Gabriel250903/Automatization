using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Automatization.Hotkeys
{
    public class HotKeyConverter : JsonConverter<HotKey>
    {
        public override HotKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var tempReader = reader;
                    Key key = Key.None;
                    ModifierKeys mod = ModifierKeys.None;
                    while (tempReader.Read())
                    {
                        if (tempReader.TokenType == JsonTokenType.EndObject) break;
                        if (tempReader.TokenType == JsonTokenType.PropertyName)
                        {
                            var propName = tempReader.GetString();
                            tempReader.Read();
                            if (string.Equals(propName, "Key", StringComparison.OrdinalIgnoreCase))
                            {
                                Enum.TryParse<Key>(tempReader.GetString(), out key);
                            }
                            else if (string.Equals(propName, "Modifiers", StringComparison.OrdinalIgnoreCase))
                            {
                                Enum.TryParse<ModifierKeys>(tempReader.GetString(), out mod);
                            }
                        }
                    }
                    reader = tempReader;
                    return new HotKey(key, mod);
                }

                return new HotKey();
            }

            string? value = reader.GetString();
            if (string.IsNullOrEmpty(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                return new HotKey();
            }

            var parts = value.Split('+');
            Key parsedKey = Key.None;
            ModifierKeys parsedModifiers = ModifierKeys.None;

            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                if (Enum.TryParse<Key>(trimmedPart, true, out var key))
                {
                    parsedKey = key;
                }
                else if (string.Equals(trimmedPart, "Ctrl", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModifiers |= ModifierKeys.Control;
                }
                else if (string.Equals(trimmedPart, "Shift", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModifiers |= ModifierKeys.Shift;
                }
                else if (string.Equals(trimmedPart, "Alt", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModifiers |= ModifierKeys.Alt;
                }
                else if (string.Equals(trimmedPart, "Win", StringComparison.OrdinalIgnoreCase))
                {
                    parsedModifiers |= ModifierKeys.Windows;
                }
            }

            return new HotKey(parsedKey, parsedModifiers);
        }

        public override void Write(Utf8JsonWriter writer, HotKey value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
