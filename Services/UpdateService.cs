using Automatization.Settings;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Automatization.Services
{
    public class UpdateService
    {
        private HttpClient _httpClient;
        private AppSettings _settings;

        public UpdateService(AppSettings settings)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Automatization-App");
            _settings = settings;
        }

        public async Task<GitHubRelease?> CheckForUpdatesAsync()
        {
            try
            {
                LogService.LogInfo("Checking for updates...");

                var response = await _httpClient.GetStringAsync($"{_settings.UpdateUrl}/latest");
                var release = JsonConvert.DeserializeObject<GitHubRelease>(response);

                if (release == null || string.IsNullOrEmpty(release.TagName))
                {
                    LogService.LogWarning("Could not retrieve release information.");
                    return null;
                }

                var latestVersion = new Version(release.TagName.TrimStart('v'));
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                if (latestVersion > currentVersion)
                {
                    LogService.LogInfo($"New version found: {latestVersion}");
                    return release;
                }

                LogService.LogInfo("Application is up to date.");

                return null;
            }
            catch (Exception ex)
            {
                LogService.LogError("An error occurred while checking for updates.", ex);
                return null;
            }
        }

        public async Task DownloadAndInstallUpdateAsync(GitHubRelease release)
        {
            var asset = release?.Assets?.FirstOrDefault(a => a.BrowserDownloadUrl != null && (a.BrowserDownloadUrl.EndsWith(".msi") || a.BrowserDownloadUrl.EndsWith(".exe")));
            
            if (asset == null || asset.BrowserDownloadUrl == null)
            {
                LogService.LogWarning("Could not find a valid installer for the latest release.");
                System.Windows.MessageBox.Show("Could not find a valid installer for the latest release.", "Update Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(asset.BrowserDownloadUrl));

            try
            {
                LogService.LogInfo($"Downloading update from {asset.BrowserDownloadUrl}");
                
                var fileBytes = await _httpClient.GetByteArrayAsync(asset.BrowserDownloadUrl);
                File.WriteAllBytes(tempPath, fileBytes);

                LogService.LogInfo($"Update downloaded to {tempPath}");

                LogService.LogInfo("Launching installer...");
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });

                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LogService.LogError("An error occurred during the update process.", ex);
                System.Windows.MessageBox.Show($"An error occurred during the update process: {ex.Message}", "Update Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string? TagName { get; set; }

        [JsonProperty("assets")]
        public GitHubAsset[]? Assets { get; set; }
    }

    public class GitHubAsset
    {
        [JsonProperty("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
