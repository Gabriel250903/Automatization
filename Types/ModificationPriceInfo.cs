namespace Automatization.Types
{
    public class ModificationPriceInfo
    {
        public string Modification { get; set; } = string.Empty;
        public int BasePrice { get; set; }
        public int DiscountAmount { get; set; }
        public int FinalPrice { get; set; }
    }
}
