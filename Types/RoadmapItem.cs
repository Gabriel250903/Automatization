using System.Text.Json.Serialization;

namespace Automatization.Types
{
    public class RoadmapItem
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("labels")]
        public List<RoadmapLabel> Labels { get; set; } = [];

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;
    }

    public class RoadmapLabel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string Color { get; set; } = string.Empty;
    }
}
