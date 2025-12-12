using Automatization.Secrets;
using Automatization.Settings;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Encoding = System.Text.Encoding;
using Json = System.Text.Json;

namespace Automatization.Services
{
    public class WebhookService(AppSettings appSettings)
    {
        private readonly string _webhookUrl = AdminSecret.GetDiscordWebhookUrl();

        public async Task<bool> SendTranslationAsync(string filePath, string nickname, string languageCode, string threadName)
        {
            if (string.IsNullOrEmpty(_webhookUrl))
            {
                LogService.LogError("Webhook URL is missing. Cannot send translation.");
                return false;
            }

            try
            {
                using HttpClient client = new();
                using MultipartFormDataContent form = [];

                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                ByteArrayContent fileContent = new(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                form.Add(fileContent, "file", Path.GetFileName(filePath));

                var payload = new
                {
                    content = $"**New Translation Submission!**\n\n👤 **Contributor:** {nickname}\n🌍 **Language:** {languageCode}",
                    username = "Translation Bot",
                    thread_name = threadName
                };

                string jsonPayload = Json.JsonSerializer.Serialize(payload);
                StringContent jsonContent = new(jsonPayload, Encoding.UTF8, "application/json");
                form.Add(jsonContent, "payload_json");

                HttpResponseMessage response = await client.PostAsync(_webhookUrl, form);

                if (response.IsSuccessStatusCode)
                {
                    LogService.LogInfo($"Translation sent successfully to Discord thread: {threadName}");
                    return true;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    LogService.LogError($"Failed to send translation. Status: {response.StatusCode}. Response: {errorResponse}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"Error sending translation webhook: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendReportAsync(string title, string description, bool isIdea, string? logFilePath = null, IEnumerable<string>? screenshotPaths = null)
        {
            if (string.IsNullOrEmpty(_webhookUrl))
            {
                LogService.LogError("Webhook URL is missing. Cannot send report.");
                return false;
            }

            string? tempLogFilePath = null;
            try
            {
                using HttpClient client = new();
                using MultipartFormDataContent form = [];

                ulong tagId = isIdea ? appSettings.DiscordIdeaTagId : appSettings.DiscordIssueTagId;

                var payload = new
                {
                    content = $"**New {(isIdea ? "Idea" : "Issue")} Report:**\n\n**Title:** {title}\n\n**Description:**\n{description}",
                    username = "Report Bot",
                    thread_name = title,
                    applied_tags = new[] { tagId }
                };

                string jsonPayload = Json.JsonSerializer.Serialize(payload);
                StringContent jsonContent = new(jsonPayload, Encoding.UTF8, "application/json");
                form.Add(jsonContent, "payload_json");

                if (!string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
                {
                    tempLogFilePath = Path.GetTempFileName();
                    File.Copy(logFilePath, tempLogFilePath, true);

                    byte[] fileBytes = await File.ReadAllBytesAsync(tempLogFilePath);
                    ByteArrayContent fileContent = new(fileBytes);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
                    form.Add(fileContent, "file", Path.GetFileName(logFilePath));
                }

                if (screenshotPaths != null)
                {
                    int i = 0;
                    foreach (string path in screenshotPaths)
                    {
                        if (File.Exists(path))
                        {
                            byte[] fileBytes = await File.ReadAllBytesAsync(path);
                            ByteArrayContent fileContent = new(fileBytes);

                            string contentType = Path.GetExtension(path).ToLower() switch
                            {
                                ".png" => "image/png",
                                ".jpg" or ".jpeg" => "image/jpeg",
                                _ => "application/octet-stream"
                            };

                            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                            form.Add(fileContent, $"file{i++}", Path.GetFileName(path));
                        }
                    }
                }

                HttpResponseMessage response = await client.PostAsync(_webhookUrl, form);

                if (response.IsSuccessStatusCode)
                {
                    LogService.LogInfo($"{(isIdea ? "Idea" : "Issue")} report sent successfully.");
                    return true;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    LogService.LogError($"Failed to send {(isIdea ? "idea" : "issue")} report. Status: {response.StatusCode}. Response: {errorResponse}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"Error sending report webhook: {ex.Message}");
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempLogFilePath) && File.Exists(tempLogFilePath))
                {
                    try
                    {
                        File.Delete(tempLogFilePath);
                    }
                    catch (Exception ex)
                    {
                        LogService.LogError($"Failed to delete temporary log file: {tempLogFilePath}. Error: {ex.Message}");
                    }
                }
            }
        }
    }
}