using Automatization.Hotkeys;
using Automatization.Services;
using Automatization.Types;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace Automatization.Settings;
public class AppSettings
{
    private static readonly JsonSerializerOptions _jsonWriteOption = new()
    {
        WriteIndented = true
    };
    private static readonly JsonSerializerOptions _jsonReadOption = new()
    {
        PropertyNameCaseInsensitive = true
    };
    public Dictionary<PowerupType, double> PowerupDelays { get; set; } = [];
    public Dictionary<PowerupType, Key> PowerupKeys { get; set; } = new()
    {
        { PowerupType.RepairKit, Key.D1 },
        { PowerupType.DoubleArmor, Key.D2 },
        { PowerupType.DoubleDamage, Key.D3 },
        { PowerupType.SpeedBoost, Key.D4 },
        { PowerupType.Mine, Key.D5 }
    };
    public HotKey GlobalHotKey { get; set; } = new(Key.F5, ModifierKeys.None);
    public HotKey RedTeamHotKey { get; set; } = new(Key.F6, ModifierKeys.None);
    public HotKey BlueTeamHotKey { get; set; } = new(Key.F7, ModifierKeys.None);
    public HotKey GoldBoxTimerHotKey { get; set; } = new(Key.F8, ModifierKeys.None);

    public HotKey SmartRepairToggleHotKey { get; set; } = new(Key.F9, ModifierKeys.None);
    public HotKey SmartRepairDebugHotKey { get; set; } = new(Key.F10, ModifierKeys.None);
    public ThemeType Theme { get; set; } = ThemeType.Dark;
    public string? GameExecutablePath { get; set; } = null;
    public double ClickSpeed { get; set; } = 10;
    public bool HotkeysPaused { get; set; } = false;
    public bool IsTimerWindowTransparent { get; set; } = false;
    public string GameProcessName { get; set; } = "ProTanki";
    public string? CustomThemeName { get; set; }
    public Point RedTeamCoordinates { get; set; } = new(1186, 1017);
    public Point BlueTeamCoordinates { get; set; } = new(1544, 1009);
    public string? CustomHealthBrightColor { get; set; }
    public string? CustomHealthDarkColor { get; set; }
    public bool UseCustomHealthColors { get; set; } = false;
    public int SmartRepairThreshold { get; set; } = 40;
    public int SmartRepairCooldown { get; set; } = 5000;
    public Key SmartRepairKey { get; set; } = Key.D1;
    public int SmartRepairFps { get; set; } = 60;
    public bool EnableAutoGoldBox { get; set; } = false;
    public string GoldBoxColor { get; set; } = "#F69001";

    public string? GetActionForHotKey(HotKey hotKey)
    {
        if (hotKey == GlobalHotKey)
        {
            return "ToggleAll";
        }

        if (hotKey == RedTeamHotKey)
        {
            return "RedTeam";
        }

        if (hotKey == BlueTeamHotKey)
        {
            return "BlueTeam";
        }

        if (hotKey == GoldBoxTimerHotKey)
        {
            return "StartTimer";
        }

        if (hotKey == SmartRepairToggleHotKey)
        {
            return "SmartRepairToggle";
        }

        return hotKey == SmartRepairDebugHotKey ? "SmartRepairDebug" : null;
    }

    public static AppSettings Load()
    {
        try
        {
            string settingsPath = GetSettingsPath();

            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);

                return JsonSerializer.Deserialize<AppSettings>(json, _jsonReadOption) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            LogService.LogError("Failed to load settings.", ex);
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string settingsPath = GetSettingsPath();
            string json = JsonSerializer.Serialize(this, _jsonWriteOption);
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception ex)
        {
            LogService.LogError("Failed to save settings.", ex);
        }
    }

    private static string GetSettingsPath()
    {
        string settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TankAutomation"
        );

        if (!Directory.Exists(settingsDir))
        {
            _ = Directory.CreateDirectory(settingsDir);
        }

        return Path.Combine(settingsDir, "appsettings.json");
    }
}
