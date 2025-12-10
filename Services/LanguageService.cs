using Automatization.Types;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace Automatization.Services
{
    public static class LanguageService
    {
        public static event EventHandler? LanguageChanged;

        private static readonly List<LanguageItem> languages =
            [
                new() { Name = "English (US)", Code = "en-US" },
                new() { Name = "Українська (UA)", Code = "uk-UA" },
                new() { Name = "Русский (RU)", Code = "ru-RU" },
                new() { Name = "Deutsch (DE)", Code = "de-DE" },
                new() { Name = "Português (PT)", Code = "pt-PT" },
                new() { Name = "Português (BR)", Code = "pt-BR" },
                new() { Name = "简体中文 (CN)", Code = "zh-CN" },
                new() { Name = "Polski (PL)", Code = "pl-PL" }
            ];

        public static void SetLanguage(string cultureCode)
        {
            ResourceDictionary dictionary = [];
            string uriPath = $"pack://application:,,,/Resources/Languages/{cultureCode}.xaml";

            try
            {
                dictionary.Source = new Uri(uriPath, UriKind.Absolute);

                ResourceDictionary? oldDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Resources/Languages/"));

                if (oldDict != null)
                {
                    _ = Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                }

                Application.Current.Resources.MergedDictionaries.Add(dictionary);

                Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureCode);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureCode);

                LanguageChanged?.Invoke(null, EventArgs.Empty);
                LogService.LogInfo($"Language changed to {cultureCode}");
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to set language to {cultureCode}: {ex.Message}");
            }
        }

        public static List<LanguageItem> GetLanguages()
        {
            return languages;
        }

        public static void LoadExistingTranslations(string langCode, ObservableCollection<TranslationItem> items)
        {
            try
            {
                string selectedLanguage = $"pack://application:,,,/Resources/Languages/{langCode}.xaml";
                ResourceDictionary listLanguages = new()
                {
                    Source = new Uri(selectedLanguage, UriKind.Absolute)
                };

                foreach (DictionaryEntry language in listLanguages)
                {
                    if (language.Key is string key && language.Value is string value)
                    {
                        TranslationItem? item = items.FirstOrDefault(x => x.Key == key);

                        if (item != null)
                        {
                            item.TranslatedValue = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to load existing language {langCode}: {ex.Message}");

                _ = new MessageBox
                {
                    Title = "Error",
                    Content = $"Could not load translations for '{langCode}'. It might not be a valid compiled language.",
                    CloseButtonText = "OK"
                }.ShowDialogAsync();
            }
        }
    }
}