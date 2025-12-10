using Automatization.Types;
using System.Collections;

namespace Automatization.Utils
{
    public class MarketItemComparer(int sortIndex) : IComparer
    {
        public int Compare(object? x, object? y)
        {
            return x is not MarketItem a || y is not MarketItem b
                ? 0
                : sortIndex switch
                {
                    0 => string.Compare(a.LocalizedName, b.LocalizedName, StringComparison.OrdinalIgnoreCase),
                    1 => string.Compare(b.LocalizedName, a.LocalizedName, StringComparison.OrdinalIgnoreCase),
                    2 => a.Prices.FirstOrDefault().CompareTo(b.Prices.FirstOrDefault()),
                    3 => b.Prices.FirstOrDefault().CompareTo(a.Prices.FirstOrDefault()),
                    _ => 0
                };
        }
    }
}