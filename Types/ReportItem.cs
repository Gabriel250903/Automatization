namespace Automatization.Types
{
    public class ReportItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsIdea { get; set; }
        public DateTime Timestamp { get; set; }
        public string? LogFilePath { get; set; } = null;
        public List<string> ScreenshotPaths { get; set; } = [];
        public ReportStatus Status { get; set; } = ReportStatus.Submitted;
    }

    public enum ReportStatus
    {
        Submitted,
        InProgress,
        Completed,
        Rejected
    }
}