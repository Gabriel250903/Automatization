namespace Automatization.Types
{
    public class ComparisonPriceInfo
    {
        public string Modification { get; set; } = string.Empty;
        public long PriceA { get; set; }
        public long PriceB { get; set; }
        public long Difference { get; set; }
    }
}
