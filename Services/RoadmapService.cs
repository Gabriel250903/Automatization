using Automatization.Types;
using System.Net.Http;
using System.Text.Json;

namespace Automatization.Services
{
    public class RoadmapService
    {
        private const string Owner = "Gabriel250903";
        private const string Repo = "Automatization";
        private const string LabelPlanned = "planned";
        private const string LabelCompleted = "completed";

        private readonly HttpClient _httpClient;

        public RoadmapService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Automatization");
        }

        public async Task<List<RoadmapItem>> GetRoadmapAsync()
        {
            try
            {
                List<RoadmapItem> allItems = [];

                string urlPlanned = $"https://api.github.com/repos/{Owner}/{Repo}/issues?labels={LabelPlanned}&state=open";
                string responsePlanned = await _httpClient.GetStringAsync(urlPlanned);
                if (!string.IsNullOrEmpty(responsePlanned))
                {
                    List<RoadmapItem>? items = JsonSerializer.Deserialize<List<RoadmapItem>>(responsePlanned);
                    if (items != null)
                    {
                        allItems.AddRange(items);
                    }
                }

                string urlCompleted = $"https://api.github.com/repos/{Owner}/{Repo}/issues?labels={LabelCompleted}&state=closed";
                string responseCompleted = await _httpClient.GetStringAsync(urlCompleted);
                if (!string.IsNullOrEmpty(responseCompleted))
                {
                    List<RoadmapItem>? items = JsonSerializer.Deserialize<List<RoadmapItem>>(responseCompleted);
                    if (items != null)
                    {
                        allItems.AddRange(items);
                    }
                }

                return allItems;
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to fetch roadmap: {ex.Message}");
                return [];
            }
        }

        public async Task<List<IssueComment>> GetIssueCommentsAsync(int issueNumber)
        {
            try
            {
                string url = $"https://api.github.com/repos/{Owner}/{Repo}/issues/{issueNumber}/comments";
                string response = await _httpClient.GetStringAsync(url);

                if (string.IsNullOrEmpty(response))
                {
                    return [];
                }

                List<IssueComment>? comments = JsonSerializer.Deserialize<List<IssueComment>>(response);
                return comments ?? [];
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to fetch comments for issue #{issueNumber}: {ex.Message}");
                return [];
            }
        }
    }
}
