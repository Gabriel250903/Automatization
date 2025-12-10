namespace Automatization.Types
{
    public class ComparisonPriceInfo
    {
        public string Modification { get; set; } = string.Empty;
        public int PriceA { get; set; }
        public int PriceB { get; set; }
        public int Difference { get; set; }
    }
}
