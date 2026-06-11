using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace Automatization.Services
{
    public class UpdaterService
    {
        private const string Owner = "Gabriel250903";
        private const string Repo = "Automatization";

        private HttpClient _httpClient;

        public UpdaterService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Automatization");
        }

        public static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0");
        }

        public async Task<(Version?, string?)> GetLatestVersionAsync()
        {
            try
            {
                string url = $"https://api.github.com/repos/{Owner}/{Repo}/releases";
                string response = await _httpClient.GetStringAsync(url);

                if (string.IsNullOrEmpty(response))
                {
                    return (null, null);
                }

                using JsonDocument jsonDoc = JsonDocument.Parse(response);

                var releases = jsonDoc.RootElement.EnumerateArray()
                    .Select(r => new
                    {
                        TagName = r.GetProperty("tag_name").GetString(),
                        ReleaseNotes = r.GetProperty("body").GetString()
                    })
                    .Where(r => !string.IsNullOrEmpty(r.TagName))
                    .Select(r =>
                    {
                        string cleanTag = r.TagName!.TrimStart('v', 'V', '.');
                        bool isParsable = Version.TryParse(cleanTag, out Version? version);
                        return new { Version = version, r.ReleaseNotes, IsValid = isParsable };
                    })
                    .Where(r => r.IsValid)
                    .OrderByDescending(r => r.Version)
                    .FirstOrDefault();

                return (releases?.Version, releases?.ReleaseNotes);
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to fetch latest version: {ex.Message}");
                return (null, null);
            }
        }

        public async Task<string?> DownloadUpdateAsync(Action<long, long> progressChanged)
        {
            LogService.LogInfo("DownloadUpdateAsync started.");

            try
            {
                string url = $"https://api.github.com/repos/{Owner}/{Repo}/releases";
                string response = await _httpClient.GetStringAsync(url);

                using JsonDocument jsonDoc = JsonDocument.Parse(response);

                var latestRelease = jsonDoc.RootElement.EnumerateArray()
                    .Select(r =>
                    {
                        string? tagName = r.GetProperty("tag_name").GetString();
                        if (string.IsNullOrEmpty(tagName))
                        {
                            return null;
                        }

                        string cleanTag = tagName.TrimStart('v', 'V', '.');
                        return Version.TryParse(cleanTag, out Version? version) ? (new { Json = r, Version = version }) : null;
                    })
                    .Where(r => r != null)
                    .OrderByDescending(r => r!.Version)
                    .FirstOrDefault();

                if (latestRelease != null)
                {
                    JsonElement assets = latestRelease.Json.GetProperty("assets");

                    foreach (JsonElement asset in assets.EnumerateArray())
                    {
                        string assetName = asset.GetProperty("name").GetString() ?? string.Empty;
                        bool isMsi = assetName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase);
                        bool isExe = assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

                        if (isMsi || isExe)
                        {
                            string? downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            if (downloadUrl == null)
                            {
                                LogService.LogError("Download URL not found.");
                                return null;
                            }

                            string targetDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TankAutomation");
                            if (!Directory.Exists(targetDirectory))
                            {
                                _ = Directory.CreateDirectory(targetDirectory);
                            }

                            string tempPath = Path.Combine(targetDirectory, assetName);
                            LogService.LogInfo($"Downloading installer from {downloadUrl} to {tempPath}");

                            using (Stream downloadStream = await _httpClient.GetStreamAsync(downloadUrl))
                            using (FileStream fileStream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                long totalBytes = asset.GetProperty("size").GetInt64();
                                byte[] buffer = new byte[8192];
                                int bytesRead = 0;
                                long totalBytesRead = 0L;

                                while ((bytesRead = await downloadStream.ReadAsync(buffer)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                                    totalBytesRead += bytesRead;
                                    progressChanged(totalBytesRead, totalBytes);
                                }
                            }

                            LogService.LogInfo("Download finished.");
                            return tempPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"Error during download: {ex.Message}");
            }

            LogService.LogInfo("No new version found or download failed.");
            return null;
        }

        public static void InstallUpdate(string filePath)
        {
            try
            {
                LogService.LogInfo($"Starting installer: {filePath}");

                ProcessStartInfo psi = new()
                {
                    FileName = filePath,
                    UseShellExecute = true
                };

                if (filePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    string logPath = Path.ChangeExtension(filePath, ".log");
                    psi.FileName = "msiexec.exe";
                    psi.Arguments = $"/i \"{filePath}\" /qf /l*v \"{logPath}\"";
                }

                _ = Process.Start(psi);

                LogService.LogInfo("Installer started. Shutting down application.");

                Thread.Sleep(1000);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to start installer: {ex.Message}");
            }
        }

        public static void RelaunchApplication()
        {
            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null)
            {
                _ = Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
            }

            Environment.Exit(0);
        }

        public static async Task CleanupUpdateFilesAsync()
        {
            try
            {
                string targetDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TankAutomation");
                if (!Directory.Exists(targetDirectory)) return;

                string[] updateFiles = Directory.GetFiles(targetDirectory, "Automatization*.*")
                    .Where(f => f.EndsWith(".msi") || f.EndsWith(".exe") || f.EndsWith(".log"))
                    .ToArray();

                if (updateFiles.Length == 0)
                {
                    return;
                }

                UpdaterService updater = new();
                (Version? latestVersion, _) = await updater.GetLatestVersionAsync();
                Version currentVersion = GetCurrentVersion();

                if (latestVersion != null && currentVersion >= latestVersion)
                {
                    foreach (string file in updateFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            LogService.LogInfo($"Deleted temporary update file: {file}");
                        }
                        catch (Exception ex)
                        {
                            LogService.LogWarning($"Could not delete {file}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    LogService.LogInfo("Update files preserved: Current version is not newer than or equal to the latest release.");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to cleanup update files: {ex.Message}");
            }
        }
    }
}