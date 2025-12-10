using Automatization.Hotkeys;
using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using System.Windows;
using Wpf.Ui.Appearance;
using Application = System.Windows.Application;
using ThemeService = Automatization.Services.ThemeService;

namespace Automatization;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = null!;
    private static readonly MarketService _marketService = new();

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        LogService.Initialize();
        LogService.LogInfo("Application starting.");

        Settings = AppSettings.Load();

        LanguageService.SetLanguage(Settings.Language);

        ThemeService.LoadThemes();

        MainWindow mainWindow = new();
        mainWindow.Show();

        if (!string.IsNullOrEmpty(Settings.CustomThemeName))
        {
            ViewModels.CustomTheme? custom = ThemeService.LoadedThemes.FirstOrDefault(t => t.Name == Settings.CustomThemeName);

            if (custom != null)
            {
                ThemeService.ApplyTheme(custom);
            }
            else
            {
                ApplyTheme(Settings.Theme);
            }
        }
        else
        {
            ApplyTheme(Settings.Theme);
        }

        _ = Task.Run(UpdaterService.CleanupUpdateFilesAsync);

        if (!ImageCacheService.IsCachePopulated())
        {
            _ = Task.Run(async () =>
            {
                IEnumerable<string> itemUrls = _marketService.Items
                    .Where(i => !string.IsNullOrEmpty(i.ImageUrl))
                    .Select(i => i.ImageUrl!);

                IEnumerable<string> rankUrls = _marketService.Ranks
                    .Where(r => !string.IsNullOrEmpty(r.Icon))
                    .Select(r => r.Icon);

                await ImageCacheService.PreloadImagesAsync(itemUrls.Concat(rankUrls));
            });
        }

        GlobalHotKeyManager.Initialize();

        LogService.LogInfo("Main window shown.");
        LogService.CleanupOldLogsAsync();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        LogService.LogInfo("Application shutting down.");
        GlobalHotKeyManager.Shutdown();
        LogService.Shutdown();
    }

    public static void ApplyTheme(ThemeType mode)
    {
        try
        {
            ApplicationTheme newTheme = mode == ThemeType.Dark
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;

            ApplicationThemeManager.Apply(newTheme);

            if (Current.MainWindow is Wpf.Ui.Controls.FluentWindow fluentWindow)
            {
                fluentWindow.WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.Mica;
            }

            LogService.LogInfo($"Theme changed to {mode}.");
        }
        catch (Exception ex)
        {
            LogService.LogError($"Failed to apply theme.", ex);
        }
    }
}