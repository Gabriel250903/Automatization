using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using Automatization.Hotkeys;
using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using Automatization.Utils;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Control = System.Windows.Controls.Control;
using ThemeService = Automatization.Services.ThemeService;

namespace Automatization;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = null!;
    private static readonly MarketService _marketService = MarketService.Instance;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        PresentationTraceSources.AnimationSource.Switch.Level = SourceLevels.Off;

        LogService.Initialize();
        LogService.LogInfo("Application starting.");

        Settings = AppSettings.Load();

        LanguageService.SetLanguage(Settings.Language);

        ThemeService.LoadThemes();

        MainWindow mainWindow = new();
        mainWindow.Show();

        if (!string.IsNullOrEmpty(Settings.CustomThemeName))
        {
            ViewModels.CustomTheme? custom = ThemeService.LoadedThemes.FirstOrDefault(t =>
                t.Name == Settings.CustomThemeName
            );

            if (custom != null)
            {
                ThemeService.ApplyTheme(custom);
                FixWpfUiButtonBindingBug();
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

        Task.Run(UpdaterService.CleanupUpdateFilesAsync)
            .SafeFireAndForget("App.CleanupUpdateFilesAsync");

        if (!ImageCacheService.IsCachePopulated())
        {
            Task.Run(async () =>
                {
                    IEnumerable<string> itemUrls = _marketService
                        .Items.Where(i => !string.IsNullOrEmpty(i.ImageUrl))
                        .Select(i => i.ImageUrl!);

                    IEnumerable<string> rankUrls = _marketService
                        .Ranks.Where(r => !string.IsNullOrEmpty(r.Icon))
                        .Select(r => r.Icon);

                    await ImageCacheService.PreloadImagesAsync(itemUrls.Concat(rankUrls));
                })
                .SafeFireAndForget("App.PreloadImagesAsync");
        }

        Task.Run(DiscordRpcService.Initialize).SafeFireAndForget("App.DiscordRpcService");

        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                try
                {
                    _ = MarketService.Instance;
                }
                catch (Exception ex)
                {
                    LogService.LogError("Warm-up background task failed.", ex);
                }
            })
        );

        LogService.LogInfo("Main window shown.");
        LogService.CleanupOldLogsAsync();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        LogService.LogInfo("Application shutting down.");
        GlobalHotKeyManager.Shutdown();
        DiscordRpcService.Shutdown();
        LogService.Shutdown();
    }

    public static void ApplyTheme(ThemeType mode)
    {
        if (Current != null && !Current.Dispatcher.CheckAccess())
        {
            _ = Current.Dispatcher.InvokeAsync(() => ApplyTheme(mode));
            return;
        }

        try
        {
            ApplicationTheme newTheme =
                mode == ThemeType.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light;

            ApplicationThemeManager.Apply(newTheme);
            FixWpfUiButtonBindingBug();

            if (Current?.MainWindow is FluentWindow fluentWindow)
            {
                fluentWindow.WindowBackdropType = WindowBackdropType.Mica;
            }

            LogService.LogInfo($"Theme changed to {mode}.");
        }
        catch (Exception ex)
        {
            LogService.LogError($"Failed to apply theme.", ex);
        }
    }

    private static bool _isWpfUiButtonBugFixed = false;

    private static void FixWpfUiButtonBindingBug()
    {
        if (_isWpfUiButtonBugFixed)
        {
            return;
        }

        try
        {
            FieldInfo? targetField = typeof(Setter).GetField(
                "_target",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (targetField == null)
            {
                return;
            }

            void PatchTemplate(ControlTemplate? template)
            {
                if (template == null)
                {
                    return;
                }

                foreach (TriggerBase trigger in template.Triggers)
                {
                    IEnumerable? setters = trigger switch
                    {
                        Trigger t => t.Setters,
                        MultiTrigger mt => mt.Setters,
                        _ => null,
                    };

                    if (setters != null)
                    {
                        foreach (Setter setter in setters.OfType<Setter>())
                        {
                            if (
                                setter.Property == TextElement.ForegroundProperty
                                && string.IsNullOrEmpty(setter.TargetName)
                                && setter.Value is System.Windows.Data.Binding binding
                                && binding.Path?.Path == "PressedForeground"
                            )
                            {
                                targetField.SetValue(setter, "ContentPresenter");
                            }
                        }
                    }
                }
            }

            void PatchStyle(Style? style)
            {
                Style? current = style;
                while (current != null)
                {
                    if (
                        current
                            .Setters.OfType<Setter>()
                            .FirstOrDefault(s => s.Property == Control.TemplateProperty)
                            ?.Value
                        is ControlTemplate template
                    )
                    {
                        PatchTemplate(template);
                    }

                    current = current.BasedOn;
                }
            }

            object[] explicitKeys =
            [
                typeof(Wpf.Ui.Controls.Button),
                "DefaultUiButtonStyle",
                "DefaultDropDownButtonStyle",
                "DefaultSplitButtonStyle",
            ];
            foreach (object key in explicitKeys)
            {
                try
                {
                    if (Application.Current.TryFindResource(key) is Style explicitStyle)
                    {
                        PatchStyle(explicitStyle);
                    }
                }
                catch { }
            }

            void PatchDictionary(ResourceDictionary? dict)
            {
                if (dict == null)
                {
                    return;
                }

                try
                {
                    foreach (object key in dict.Keys)
                    {
                        try
                        {
                            if (
                                key is string s
                                && (
                                    s.EndsWith("ButtonStyle", StringComparison.OrdinalIgnoreCase)
                                    || s.Equals(
                                        "DefaultUiButton",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                            )
                            {
                                if (dict[key] is Style style)
                                {
                                    PatchStyle(style);
                                }
                            }
                            else if (key is Type t && typeof(Button).IsAssignableFrom(t))
                            {
                                if (dict[key] is Style style)
                                {
                                    PatchStyle(style);
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                try
                {
                    foreach (ResourceDictionary sub in dict.MergedDictionaries)
                    {
                        PatchDictionary(sub);
                    }
                }
                catch { }
            }

            PatchDictionary(Application.Current.Resources);
            _isWpfUiButtonBugFixed = true;
        }
        catch (Exception ex)
        {
            LogService.LogWarning($"Could not patch Wpf.Ui button style: {ex.Message}");
        }
    }
}
