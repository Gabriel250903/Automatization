using Application = System.Windows.Application;

namespace Automatization.Types
{
    public class MarketItem(string name, ItemCategory category, int[] prices, string? imageUrl = null, string? description = null)
    {
        public string Name { get; set; } = name;
        public ItemCategory Category { get; set; } = category;
        public int[] Prices { get; set; } = prices;
        public string? ImageUrl { get; set; } = imageUrl;
        public string? Description { get; set; } = description;
        public string LocalizedName => Application.Current?.TryFindResource(Name) as string ?? Name;
        public string? LocalizedDescription => Description != null ? Application.Current?.TryFindResource(Description) as string ?? Description : null;
    }
}