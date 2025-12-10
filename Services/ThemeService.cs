using Automatization.ViewModels;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using BackgroundType = Automatization.Types.BackgroundType;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace Automatization.Services
{
    public static class ThemeService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        private static string ThemeFilePath
        {
            get
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TankAutomation");
                if (!Directory.Exists(directory))
                {
                    _ = Directory.CreateDirectory(directory);
                }

                return Path.Combine(directory, "custom_themes.json");
            }
        }

        public static List<CustomTheme> LoadedThemes { get; private set; } = [];

        public static void LoadThemes()
        {
            if (File.Exists(ThemeFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ThemeFilePath);
                    LoadedThemes = JsonSerializer.Deserialize<List<CustomTheme>>(json) ?? [];
                }
                catch { LoadedThemes = []; }
            }
            else { LoadedThemes = []; }
        }

        public static void SaveTheme(CustomTheme theme)
        {
            CustomTheme? existing = LoadedThemes.FirstOrDefault(x => x.Name == theme.Name);
            if (existing != null)
            {
                _ = LoadedThemes.Remove(existing);
            }

            LoadedThemes.Add(theme);
            try { File.WriteAllText(ThemeFilePath, JsonSerializer.Serialize(LoadedThemes, _jsonOptions)); } catch { }
        }

        public static void DeleteTheme(CustomTheme theme)
        {
            CustomTheme? existing = LoadedThemes.FirstOrDefault(x => x.Name == theme.Name);
            if (existing != null)
            {
                _ = LoadedThemes.Remove(existing);
                try { File.WriteAllText(ThemeFilePath, JsonSerializer.Serialize(LoadedThemes, _jsonOptions)); } catch { }
            }
        }

        public static void ClearThemeOverrides()
        {
            ResourceDictionary resources = Application.Current.Resources;

            resources.Remove("TextFillColorPrimaryBrush");
            resources.Remove("TextFillColorSecondaryBrush");
            resources.Remove("ControlFillColorDefaultBrush");
            resources.Remove("ControlFillColorSecondaryBrush");
            resources.Remove("SystemAccentColorPrimaryBrush");
            resources.Remove("ApplicationBackgroundBrush");
            resources.Remove("WindowBackground");

            foreach (Window window in Application.Current.Windows)
            {
                if (window is FluentWindow fw)
                {
                    fw.WindowBackdropType = WindowBackdropType.Mica;
                    fw.Background = Brushes.Transparent;
                }
            }
        }

        public static void RefreshActiveWindow(Window window)
        {
            _ = window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (Application.Current.Resources.Contains("ApplicationBackgroundBrush") &&
                    Application.Current.Resources["ApplicationBackgroundBrush"] is Brush bgBrush)
                {
                    if (window is FluentWindow fw)
                    {
                        fw.WindowBackdropType = WindowBackdropType.None;
                        fw.Background = bgBrush;
                    }
                }
            }));
        }

        public static void ApplyTheme(CustomTheme theme, ResourceDictionary? targetResources = null)
        {
            ResourceDictionary resources = targetResources ?? Application.Current.Resources;

            if (targetResources == null)
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
            }

            static SolidColorBrush GetSolid(string hex)
            {
                try
                {
                    SolidColorBrush brush = new((Color)ColorConverter.ConvertFromString(hex));
                    if (brush.CanFreeze)
                    {
                        brush.Freeze();
                    }

                    return brush;
                }
                catch { return Brushes.Red; }
            }

            resources["TextFillColorPrimaryBrush"] = GetSolid(theme.TextColor);
            resources["TextFillColorSecondaryBrush"] = GetSolid(theme.TextColor);
            resources["ControlFillColorDefaultBrush"] = GetSolid(theme.ButtonBackgroundColor);
            resources["ControlFillColorSecondaryBrush"] = GetSolid(theme.ButtonHoverColor);
            resources["SystemAccentColorPrimaryBrush"] = GetSolid(theme.AccentColor);

            Brush bgBrush;
            switch (theme.BackgroundMode)
            {
                case BackgroundType.Image:
                    if (!string.IsNullOrEmpty(theme.BackgroundImagePath) && File.Exists(theme.BackgroundImagePath))
                    {
                        BitmapImage img = new(new Uri(theme.BackgroundImagePath, UriKind.Absolute));
                        if (img.CanFreeze)
                        {
                            img.Freeze();
                        }

                        bgBrush = new ImageBrush(img) { Stretch = Stretch.UniformToFill };
                    }
                    else
                    {
                        bgBrush = GetSolid(theme.WindowBackgroundColor);
                    }

                    break;
                case BackgroundType.Gradient:
                    try
                    {
                        Color start = (Color)ColorConverter.ConvertFromString(theme.WindowBackgroundColor);
                        Color end = (Color)ColorConverter.ConvertFromString(theme.WindowGradientEndColor);
                        bgBrush = new LinearGradientBrush(start, end, 45.0);
                    }
                    catch { bgBrush = GetSolid(theme.WindowBackgroundColor); }
                    break;
                default:
                    bgBrush = GetSolid(theme.WindowBackgroundColor);
                    break;
            }

            if (bgBrush.CanFreeze)
            {
                bgBrush.Freeze();
            }

            resources["ApplicationBackgroundBrush"] = bgBrush;
            resources["WindowBackground"] = bgBrush;

            if (targetResources == null)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is FluentWindow fw)
                        {
                            fw.WindowBackdropType = WindowBackdropType.None;
                            fw.Background = bgBrush;
                        }
                    }
                }));
            }
        }
    }
}