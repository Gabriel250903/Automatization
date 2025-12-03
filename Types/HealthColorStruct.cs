namespace Automatization.Types
{
    public struct HealthColorStruct(string brightHex, string darkHex, TeamMode mode)
    {
        public Color Bright = ColorTranslator.FromHtml(brightHex);
        public Color Dark = ColorTranslator.FromHtml(darkHex);
        public TeamMode Mode = mode;
    }
}
