using System.Windows;
using Automatization.Settings;
using Automatization.Types;

namespace Automatization;
public partial class App : System.Windows.Application
{
    public static AppSettings Settings { get; private set; } = null!;

    private static ResourceDictionary? _currentThemeDictionary;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        Settings = AppSettings.Load();

        ApplyTheme(Settings.Theme);

        MainWindow mainWindow = new();
        mainWindow.Show();
    }

    public static void ApplyTheme(ThemeType mode)
    {
        string themePath = mode == ThemeType.Light
            ? "Themes/Light.xaml"
            : "Themes/Dark.xaml";

        try
        {
            var dict = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };

            if (_currentThemeDictionary != null)
            {
                Current.Resources.MergedDictionaries.Remove(_currentThemeDictionary);
            }

            Current.Resources.MergedDictionaries.Add(dict);
            _currentThemeDictionary = dict;
        }
        catch (Exception)
        {
        }
    }
}
