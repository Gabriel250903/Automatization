using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Automatization.Services
{
    public static class ImageCacheService
    {
        public static string CacheDirectory { get; } =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TankAutomation",
                "Cache"
            );
        private static readonly HttpClient HttpClient = new();
        private static readonly ConcurrentDictionary<string, Task<string?>> _inFlightDownloads =
            new();

        static ImageCacheService()
        {
            if (!Directory.Exists(CacheDirectory))
            {
                _ = Directory.CreateDirectory(CacheDirectory);
            }
        }

        private static async Task<string?> DownloadAndSaveAtomicAsync(
            string url,
            string destinationPath
        )
        {
            return File.Exists(destinationPath)
                ? destinationPath
                : await _inFlightDownloads
                    .GetOrAdd(
                        destinationPath,
                        async (dest) =>
                        {
                            try
                            {
                                if (File.Exists(dest))
                                {
                                    return dest;
                                }

                                byte[] data = await HttpClient
                                    .GetByteArrayAsync(url)
                                    .ConfigureAwait(false);
                                string tempFile =
                                    dest + "." + Guid.NewGuid().ToString("N") + ".tmp";

                                await File.WriteAllBytesAsync(tempFile, data).ConfigureAwait(false);
                                File.Move(tempFile, dest, overwrite: true);

                                return dest;
                            }
                            catch (Exception ex)
                            {
                                LogService.LogError(
                                    $"Failed to download or cache image from {url}: {ex.Message}"
                                );
                                return null;
                            }
                            finally
                            {
                                _ = _inFlightDownloads.TryRemove(dest, out _);
                            }
                        }
                    )
                    .ConfigureAwait(false);
        }

        public static bool IsCachePopulated()
        {
            return Directory.Exists(CacheDirectory)
                && Directory.EnumerateFiles(CacheDirectory).Any();
        }

        public static async Task<string?> GetCachedImagePathAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            string fileName = GetHashString(url) + ".png";
            string localPath = Path.Combine(CacheDirectory, fileName);

            return await DownloadAndSaveAtomicAsync(url, localPath);
        }

        public static string? GetCachedImagePathNonBlocking(string url, out string localPath)
        {
            if (string.IsNullOrEmpty(url))
            {
                localPath = string.Empty;
                return null;
            }

            string fileName = GetHashString(url) + ".png";
            localPath = Path.Combine(CacheDirectory, fileName);

            if (File.Exists(localPath))
            {
                return localPath;
            }

            _ = DownloadAndSaveAtomicAsync(url, localPath);
            return null;
        }

        public static async Task<string?> GetIssueImagePathAsync(string url, int issueId)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            string issueDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TankAutomation",
                "Issues",
                issueId.ToString(),
                "Images"
            );

            if (!Directory.Exists(issueDir))
            {
                _ = Directory.CreateDirectory(issueDir);
            }

            string fileName = GetHashString(url) + GetSafeExtensionFromUrl(url);
            string localPath = Path.Combine(issueDir, fileName);

            return await DownloadAndSaveAtomicAsync(url, localPath);
        }

        public static string? GetIssueImagePathNonBlocking(
            string url,
            int issueId,
            out string localPath
        )
        {
            if (string.IsNullOrEmpty(url))
            {
                localPath = string.Empty;
                return null;
            }

            string issueDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TankAutomation",
                "Issues",
                issueId.ToString(),
                "Images"
            );

            if (!Directory.Exists(issueDir))
            {
                _ = Directory.CreateDirectory(issueDir);
            }

            string fileName = GetHashString(url) + GetSafeExtensionFromUrl(url);
            localPath = Path.Combine(issueDir, fileName);

            if (File.Exists(localPath))
            {
                return localPath;
            }

            _ = DownloadAndSaveAtomicAsync(url, localPath);
            return null;
        }

        public static async Task PreloadImagesAsync(IEnumerable<string> urls)
        {
            IEnumerable<Task> tasks = urls.Distinct()
                .Select(url =>
                {
                    if (string.IsNullOrEmpty(url))
                    {
                        return Task.CompletedTask;
                    }

                    string fileName = GetHashString(url) + ".png";
                    string localPath = Path.Combine(CacheDirectory, fileName);

                    return DownloadAndSaveAtomicAsync(url, localPath);
                });

            await Task.WhenAll(tasks);
        }

        private static string GetHashString(string inputString)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(inputString));
            StringBuilder sb = new();

            foreach (byte b in bytes)
            {
                _ = sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        private static string GetSafeExtensionFromUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                string ext = Path.GetExtension(uri.AbsolutePath);
                if (
                    !string.IsNullOrEmpty(ext)
                    && ext.Length <= 5
                    && !ext.Any(c => Path.GetInvalidFileNameChars().Contains(c))
                )
                {
                    return ext;
                }
            }
            return ".png";
        }
    }
}
