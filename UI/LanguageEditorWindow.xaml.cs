using Automatization.Services;
using Automatization.Settings;
using Automatization.Types;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace Automatization.UI
{
    public partial class LanguageEditorWindow : FluentWindow
    {
        public ObservableCollection<TranslationItem> Items { get; set; } = [];

        public LanguageEditorWindow(string? existingLanguageCode = null)
        {
            InitializeComponent();
            LoadEnglishTemplate();

            List<string> categories = Items.Select(i => i.Category).Distinct().OrderBy(c => c).ToList();
            categories.Insert(0, "All Categories");
            CmbCategories.ItemsSource = categories;
            CmbCategories.SelectedIndex = 0;

            TranslationGrid.ItemsSource = Items;

            if (!string.IsNullOrEmpty(existingLanguageCode))
            {
                TxtLangCode.Text = existingLanguageCode;
                TxtLangCode.IsReadOnly = true;
                BtnBrowseLang.IsEnabled = false;
                Title = $"Language Editor - Editing {existingLanguageCode}";
                LanguageService.LoadExistingTranslations(existingLanguageCode, Items);
            }
        }

        private void LoadEnglishTemplate()
        {
            try
            {
                string uriPath = "pack://application:,,,/Resources/Languages/en-US.xaml";
                ResourceDictionary dictionary = new()
                {
                    Source = new Uri(uriPath, UriKind.Absolute)
                };

                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is string key && entry.Value is string value)
                    {
                        string category = "General";
                        int underscoreIndex = key.IndexOf('_');

                        if (underscoreIndex > 0)
                        {
                            category = key[..underscoreIndex];
                        }

                        Items.Add(new TranslationItem
                        {
                            Key = key,
                            Category = category,
                            OriginalValue = value,
                            TranslatedValue = value
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to load English template: {ex.Message}");
                _ = new MessageBox
                {
                    Title = "Error",
                    Content = "Failed to load the base English language file.",
                    CloseButtonText = "Close"
                }.ShowDialogAsync();
            }
        }

        private string GenerateXamlContent()
        {
            string header = "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                            "                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                            "                    xmlns:system=\"clr-namespace:System;assembly=mscorlib\">\n\n";

            string footer = "</ResourceDictionary>";

            System.Text.StringBuilder sb = new();
            _ = sb.Append(header);

            IOrderedEnumerable<TranslationItem> sortedItems = Items.OrderBy(x => x.Key);

            foreach (TranslationItem item in sortedItems)
            {
                string safeValue = System.Security.SecurityElement.Escape(item.TranslatedValue);
                _ = sb.AppendLine($"    <system:String x:Key=\"{item.Key}\">{safeValue}</system:String>");
            }

            _ = sb.Append(footer);
            return sb.ToString();
        }

        private void CmbCategories_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CmbCategories.SelectedItem is string selectedCategory)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(Items);

                view.Filter = selectedCategory switch
                {
                    "All Categories" => null,
                    _ => item => item is TranslationItem t && t.Category == selectedCategory,
                };

                view.Refresh();
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            string langCode = TxtLangCode.Text.Trim();
            string nickname = TxtNickname.Text.Trim();

            if (string.IsNullOrEmpty(langCode))
            {
                await ShowError("Please enter a Target Language Code (e.g. ro-RO).");
                return;
            }

            if (string.IsNullOrEmpty(nickname))
            {
                await ShowError("Please enter your Nickname.");
                return;
            }

            string tempFile = Path.Combine(Path.GetTempPath(), $"{langCode}.xaml");
            try
            {
                string xamlContent = GenerateXamlContent();
                await File.WriteAllTextAsync(tempFile, xamlContent);
                AppSettings appSettings = AppSettings.Load();

                BtnSend.IsEnabled = false;
                BtnSend.Content = "Sending...";

                WebhookService webhook = new(appSettings);
                string threadName = $"Translation: {langCode} by {nickname}";

                bool success = await webhook.SendTranslationAsync(tempFile, nickname, langCode, threadName);

                try
                {
                    if (success)
                    {
                        _ = new MessageBox
                        {
                            Title = "Success!",
                            Content = "Thank you! Your translation has been sent to the developer.",
                            CloseButtonText = "Awesome"
                        }.ShowDialogAsync();
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    LogService.LogError($"Exception while sending translation.", ex);
                    await ShowError("Failed to send translation. Please check your internet connection or try again later.");
                }
            }
            catch (Exception ex)
            {
                await ShowError($"An error occurred: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                BtnSend.IsEnabled = true;
            }
        }

        private async void BtnSaveLocal_Click(object sender, RoutedEventArgs e)
        {
            string langCode = TxtLangCode.Text.Trim();
            if (string.IsNullOrEmpty(langCode))
            {
                langCode = "custom-language";
            }

            Microsoft.Win32.SaveFileDialog dlg = new()
            {
                FileName = $"{langCode}.xaml",
                Filter = "XAML Resource Dictionary|*.xaml",
                Title = "Save Translation File"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string xamlContent = GenerateXamlContent();
                    await File.WriteAllTextAsync(dlg.FileName, xamlContent);
                    LogService.LogInfo($"Translation saved locally to {dlg.FileName}");
                }
                catch (Exception ex)
                {
                    await ShowError($"Failed to save file: {ex.Message}");
                }
            }
        }

        private static async Task ShowError(string message)
        {
            _ = await new MessageBox
            {
                Title = "Validation",
                Content = message,
                CloseButtonText = "OK"
            }.ShowDialogAsync();
        }

        private void BtnBrowseLang_Click(object sender, RoutedEventArgs e)
        {
            LanguagePickerWindow picker = new() { Owner = this };

            if (picker.ShowDialog() == true && !string.IsNullOrEmpty(picker.SelectedLanguageCode))
            {
                TxtLangCode.Text = picker.SelectedLanguageCode;
            }
        }
    }
}
