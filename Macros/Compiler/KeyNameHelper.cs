using System.Windows.Input;

namespace Automatization.Macros.Compiler
{
    public static class KeyNameHelper
    {
        public static ushort ParseVirtualKey(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return 0;
            }

            keyName = keyName.Trim().Trim('"', '\'');

            switch (keyName.ToUpperInvariant())
            {
                case "0" or "NUM0" or "NUMPAD0":
                    return 0x30;
                case "1" or "NUM1" or "NUMPAD1":
                    return 0x31;
                case "2" or "NUM2" or "NUMPAD2":
                    return 0x32;
                case "3" or "NUM3" or "NUMPAD3":
                    return 0x33;
                case "4" or "NUM4" or "NUMPAD4":
                    return 0x34;
                case "5" or "NUM5" or "NUMPAD5":
                    return 0x35;
                case "6" or "NUM6" or "NUMPAD6":
                    return 0x36;
                case "7" or "NUM7" or "NUMPAD7":
                    return 0x37;
                case "8" or "NUM8" or "NUMPAD8":
                    return 0x38;
                case "9" or "NUM9" or "NUMPAD9":
                    return 0x39;
                case "SPACE":
                    return 0x20;
                case "ENTER" or "RETURN":
                    return 0x0D;
                case "ESC" or "ESCAPE":
                    return 0x1B;
                case "TAB":
                    return 0x09;
                case "SHIFT":
                    return 0x10;
                case "LSHIFT":
                    return 0xA0;
                case "RSHIFT":
                    return 0xA1;
                case "CTRL" or "CONTROL":
                    return 0x11;
                case "LCTRL" or "LCONTROL":
                    return 0xA2;
                case "RCTRL" or "RCONTROL":
                    return 0xA3;
                case "ALT":
                    return 0x12;
                case "LALT":
                    return 0xA4;
                case "RALT":
                    return 0xA5;
                case "BACK" or "BACKSPACE":
                    return 0x08;
                case "CAPS" or "CAPSLOCK":
                    return 0x14;
                case "UP":
                    return 0x26;
                case "DOWN":
                    return 0x28;
                case "LEFT":
                    return 0x25;
                case "RIGHT":
                    return 0x27;
                case "INSERT":
                    return 0x2D;
                case "DELETE" or "DEL":
                    return 0x2E;
                case "HOME":
                    return 0x24;
                case "END":
                    return 0x23;
                case "PAGEUP" or "PGUP":
                    return 0x21;
                case "PAGEDOWN" or "PGDN":
                    return 0x22;
                case "PRINTSCREEN" or "PRTSCN" or "SNAPSHOT":
                    return 0x2C;
                case "SCROLL" or "SCROLLLOCK":
                    return 0x91;
                case "PAUSE":
                    return 0x13;
                case "NUMLOCK":
                    return 0x90;

                case "-"
                or "MINUS":
                    return 0xBD;
                case "+" or "PLUS" or "=" or "EQUAL" or "EQUALS":
                    return 0xBB;
                case "[" or "LBRACKET" or "OPENBRACKET":
                    return 0xDB;
                case "]" or "RBRACKET" or "CLOSEBRACKET":
                    return 0xDD;
                case ";" or "SEMICOLON":
                    return 0xBA;
                case "'" or "QUOTE" or "APOSTROPHE":
                    return 0xDE;
                case "," or "COMMA":
                    return 0xBC;
                case "." or "PERIOD" or "DOT":
                    return 0xBE;
                case "/" or "SLASH":
                    return 0xBF;
                case "\\" or "BACKSLASH":
                    return 0xDC;
                case "`" or "TILDE" or "BACKTICK":
                    return 0xC0;

                case "MULTIPLY"
                or "NUMPAD_MULTIPLY"
                or "*":
                    return 0x6A;
                case "ADD" or "NUMPAD_ADD":
                    return 0x6B;
                case "SUBTRACT" or "NUMPAD_SUBTRACT":
                    return 0x6D;
                case "DECIMAL" or "NUMPAD_DECIMAL":
                    return 0x6E;
                case "DIVIDE" or "NUMPAD_DIVIDE":
                    return 0x6F;
            }

            if (keyName.Length == 1 && char.IsLetter(keyName[0]))
            {
                return char.ToUpperInvariant(keyName[0]);
            }

            if (Enum.TryParse(keyName, true, out Key wpfKey))
            {
                int vk = KeyInterop.VirtualKeyFromKey(wpfKey);
                if (vk > 0)
                {
                    return (ushort)vk;
                }
            }

            return 0;
        }

        public static bool TryParseVirtualKey(string keyName, out ushort vk)
        {
            vk = ParseVirtualKey(keyName);
            return vk != 0;
        }

        public static Key VirtualKeyToKey(ushort vk)
        {
            return KeyInterop.KeyFromVirtualKey(vk);
        }
    }
}
