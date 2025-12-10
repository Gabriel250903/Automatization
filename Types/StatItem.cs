namespace Automatization.Types
{
    public class StatItem
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? RankStartIcon { get; set; }
        public string? RankEndIcon { get; set; }
        public bool IsRank { get; set; }
    }
}
