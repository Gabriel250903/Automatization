namespace Automatization.Types
{
    public class ModificationPriceInfo
    {
        public string Modification { get; set; } = string.Empty;
        public long BasePrice { get; set; }
        public long DiscountAmount { get; set; }
        public long FinalPrice { get; set; }
    }
}
