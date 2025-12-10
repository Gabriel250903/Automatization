using Automatization.Types;
using System.IO;
using System.Text.Json;

namespace Automatization.Services
{
    public class ReportService
    {
        private const string REPORTS_FILE_NAME = "reports.json";

        private static readonly JsonSerializerOptions _jsonWriteOption = new()
        {
            WriteIndented = true
        };
        private static readonly JsonSerializerOptions _jsonReadOption = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static List<ReportItem> LoadReports()
        {
            List<ReportItem> reports = [];
            string reportsPath = GetReportsPath();
            LogService.LogInfo($"Attempting to load reports from: {reportsPath}");

            try
            {
                if (File.Exists(reportsPath))
                {
                    LogService.LogInfo($"reports.json found at: {reportsPath}");
                    string json = File.ReadAllText(reportsPath);
                    reports = JsonSerializer.Deserialize<List<ReportItem>>(json, _jsonReadOption) ?? [];
                    LogService.LogInfo($"Loaded {reports.Count} reports.");
                }
                else
                {
                    LogService.LogInfo($"reports.json not found at: {reportsPath}. Returning empty list.");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to load reports.", ex);
            }

            return reports;
        }

        public static void SaveReports(List<ReportItem> reports)
        {
            try
            {
                string reportsPath = GetReportsPath();
                string json = JsonSerializer.Serialize(reports, _jsonWriteOption);
                File.WriteAllText(reportsPath, json);
            }
            catch (Exception ex)
            {
                LogService.LogError("Failed to save reports.", ex);
            }
        }

        public static void AddReport(ReportItem newReport)
        {
            List<ReportItem> reports = LoadReports();
            reports.Add(newReport);
            SaveReports(reports);
        }

        private static string GetReportsPath()
        {
            string settingsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TankAutomation"
            );

            if (!Directory.Exists(settingsDir))
            {
                _ = Directory.CreateDirectory(settingsDir);
            }

            return Path.Combine(settingsDir, REPORTS_FILE_NAME);
        }
    }
}
