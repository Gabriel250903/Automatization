using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using Automatization.Utils;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using ThemeService = Automatization.Services.ThemeService;

namespace Automatization;

public partial class App : System.Windows.Application
{
    public static AppSettings Settings { get; private set; } = null!;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        LogService.Initialize();
        LogService.LogInfo("Application starting.");

        Settings = AppSettings.Load();

        ThemeService.LoadThemes();

        MainWindow mainWindow = new();
        mainWindow.Show();

        if (!string.IsNullOrEmpty(Settings.CustomThemeName))
        {
            ViewModels.CustomTheme? custom = ThemeService.LoadedThemes.FirstOrDefault(t => t.Name == Settings.CustomThemeName);

            if (custom != null) ThemeService.ApplyTheme(custom);
            else ApplyTheme(Settings.Theme);
        }
        else ApplyTheme(Settings.Theme);

        LogService.LogInfo("Main window shown.");
        LogService.CleanupOldLogsAsync();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        LogService.LogInfo("Application shutting down.");
        LogService.Shutdown();
    }

    public static void ApplyTheme(ThemeType mode)
    {
        try
        {
            ApplicationTheme newTheme = mode == ThemeType.Light
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark;

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