namespace Automatization.Types
{
    public class DetectionResultStruct
    {
        public bool Success { get; set; }
        public bool ColorDetected { get; set; }
        public string? DetectedText { get; set; }
        public string? Error { get; set; }
    }
}
