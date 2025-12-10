using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Automatization.Services
{
    public static class ImageCacheService
    {
        public static string CacheDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TankAutomation", "Cache");
        private static readonly HttpClient HttpClient = new();

        static ImageCacheService()
        {
            if (!Directory.Exists(CacheDirectory))
            {
                _ = Directory.CreateDirectory(CacheDirectory);
            }
        }

        public static bool IsCachePopulated()
        {
            return Directory.Exists(CacheDirectory) && Directory.EnumerateFiles(CacheDirectory).Any();
        }

        public static string? GetCachedImagePath(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            string fileName = GetHashString(url) + ".png";
            string localPath = Path.Combine(CacheDirectory, fileName);

            if (File.Exists(localPath))
            {
                return localPath;
            }

            try
            {
                byte[] data = HttpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                File.WriteAllBytes(localPath, data);
                return localPath;
            }
            catch
            {
                return null;
            }
        }

        public static string? GetIssueImagePath(string url, int issueId)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            string issueDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TankAutomation", "Issues", issueId.ToString(), "Images");

            if (!Directory.Exists(issueDir))
            {
                _ = Directory.CreateDirectory(issueDir);
            }

            string fileName = GetHashString(url) + Path.GetExtension(url);
            if (string.IsNullOrEmpty(Path.GetExtension(url)))
            {
                fileName += ".png";
            }

            string localPath = Path.Combine(issueDir, fileName);

            if (File.Exists(localPath))
            {
                return localPath;
            }

            try
            {
                byte[] data = HttpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                File.WriteAllBytes(localPath, data);
                return localPath;
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to download issue image: {ex.Message}");
                return null;
            }
        }

        public static async Task PreloadImagesAsync(IEnumerable<string> urls)
        {
            IEnumerable<Task> tasks = urls.Distinct().Select(async url =>
            {
                if (string.IsNullOrEmpty(url))
                {
                    return;
                }

                string fileName = GetHashString(url) + ".png";
                string localPath = Path.Combine(CacheDirectory, fileName);

                if (!File.Exists(localPath))
                {
                    try
                    {
                        byte[] data = await HttpClient.GetByteArrayAsync(url);
                        await File.WriteAllBytesAsync(localPath, data);
                    }
                    catch
                    {
                        LogService.LogError($"Failed to preload image from URL: {url}");
                    }
                }
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
    }
}
