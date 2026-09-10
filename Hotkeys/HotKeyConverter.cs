using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Automatization.Hotkeys
{
    public class HotKeyConverter : JsonConverter<HotKey>
    {
        public override HotKey Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    Utf8JsonReader tempReader = reader;
                    Key key = Key.None;
                    ModifierKeys mod = ModifierKeys.None;
                    while (tempReader.Read())
                    {
                        if (tempReader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (tempReader.TokenType == JsonTokenType.PropertyName)
                        {
                            string? propName = tempReader.GetString();
                            _ = tempReader.Read();

                            if (string.Equals(propName, "Key", StringComparison.OrdinalIgnoreCase))
                            {
                                _ = Enum.TryParse<Key>(tempReader.GetString(), out key);
                            }
                            else if (
                                string.Equals(
                                    propName,
                                    "Modifiers",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                _ = Enum.TryParse<ModifierKeys>(tempReader.GetString(), out mod);
                            }
                        }
                    }

                    reader = tempReader;
                    return new HotKey(key, mod);
                }

                return new HotKey();
            }

            string? value = reader.GetString();
            return HotKey.TryParse(value, out HotKey? parsed) ? parsed : new HotKey();
        }

        public override void Write(
            Utf8JsonWriter writer,
            HotKey value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
