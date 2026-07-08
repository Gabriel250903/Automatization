using Automatization.Settings;
using DiscordRPC;

namespace Automatization.Services
{
    public static class DiscordRpcService
    {
        private static DiscordRpcClient? _client;
        private static DateTime? _startTime;

        public static void Initialize()
        {
            if (_client != null)
            {
                return;
            }

            AppSettings settings = AppSettings.Load();
            if (!settings.EnableDiscordRpc)
            {
                return;
            }

            try
            {
                _client = new DiscordRpcClient(settings.DiscordRpcAppId);
                _ = _client.Initialize();
                _startTime = DateTime.UtcNow;
                UpdatePresence(false);
                LogService.LogInfo("Discord RPC initialized.");
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to initialize Discord RPC.", ex);
            }
        }

        public static void UpdatePresence(bool isGameRunning)
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                AppSettings settings = AppSettings.Load();
                string statusText = isGameRunning ? "Running" : "Not Running";

                string details = settings.DiscordRpcDetails.Replace("{GameStatus}", statusText);
                string state = settings.DiscordRpcState.Replace("{GameStatus}", statusText);

                _client.SetPresence(new RichPresence
                {
                    Details = details,
                    State = state,
                    Timestamps = _startTime.HasValue ? new Timestamps { Start = _startTime.Value } : null,
                    Assets = new Assets
                    {
                        LargeImageKey = "icon",
                        LargeImageText = "Automatization"
                    }
                });
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to update Discord RPC presence.", ex);
            }
        }

        public static void Shutdown()
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                _client.Dispose();
                _client = null;
                LogService.LogInfo("Discord RPC shut down.");
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to shut down Discord RPC.", ex);
            }
        }
    }
}
