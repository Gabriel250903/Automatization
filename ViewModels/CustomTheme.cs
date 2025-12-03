using BackgroundType = Automatization.Types.BackgroundType;

namespace Automatization.ViewModels
{
    public class CustomTheme
    {
        public string Name { get; set; } = "Untitled";

        public string TextColor { get; set; } = "#FF000000";
        public string ButtonBackgroundColor { get; set; } = "#33000000";
        public string ButtonHoverColor { get; set; } = "#55000000";
        public string AccentColor { get; set; } = "#FF0078D7";

        public BackgroundType BackgroundMode { get; set; } = BackgroundType.Solid;

        public string WindowBackgroundColor { get; set; } = "#FFFFFFFF";

        public string WindowGradientEndColor { get; set; } = "#FFDDDDDD";

        public string? BackgroundImagePath { get; set; } = null;
    }
}
