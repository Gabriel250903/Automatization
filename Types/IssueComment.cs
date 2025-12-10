using System.Text.Json.Serialization;

namespace Automatization.Types
{
    public class IssueComment
    {
        public int ParentIssueNumber { get; set; } // Added to associate comment with its parent issue

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public GitHubUser User { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class GitHubUser
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
