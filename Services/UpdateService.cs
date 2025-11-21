using AutoUpdaterDotNET;
using System.Reflection;

namespace Automatization.Services
{
    public class UpdateService
    {
        private const string UpdateXmlUrl = "https://github.com/Gabriel250903/Automatization/releases/latest/download/update.xml";

        public void CheckForUpdates()
        {
            AutoUpdater.Start(UpdateXmlUrl);
        }

        public static string? GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        }
    }
}
