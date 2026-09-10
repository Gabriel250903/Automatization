using Automatization.Services;

namespace Automatization.Macros.Models
{
    public class MacroPackage
    {
        public const string Header = "Automatization Macro";
        public const string FileExtension = ".azmacro";
        public const string FileFilter = "Automatization Macro (*.azmacro)|*.azmacro";

        public string? Magic { get; set; } = Header;
        public int Version { get; set; } = 1;
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public string AppVersion { get; set; } = UpdaterService.GetCurrentVersion().ToString();

        public MacroDefinition? Macro { get; set; }
        public string? Checksum { get; set; }
    }
}
