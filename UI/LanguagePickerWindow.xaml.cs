using Automatization.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace Automatization.UI
{
    public partial class LanguagePickerWindow : FluentWindow
    {
        public string? SelectedLanguageCode { get; private set; }

        private readonly ObservableCollection<CultureInfo> _allLanguages = [];
        private readonly ICollectionView _filteredLanguages;
        private readonly HashSet<string> _targetLanguageCodes;
        private readonly bool _showImplementedLanguagesOnly;

        public LanguagePickerWindow(bool showImplementedOnly = false)
        {
            InitializeComponent();
            _showImplementedLanguagesOnly = showImplementedOnly;

            Title = _showImplementedLanguagesOnly ? "Select Language to Edit" : "Select Language to Add";

            IEnumerable<string> implementedLangs = LanguageService.GetLanguages().Select(li => li.Code);
            _targetLanguageCodes = new HashSet<string>(implementedLangs, StringComparer.OrdinalIgnoreCase);

            CultureInfo[] languages = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            foreach (CultureInfo language in languages.OrderBy(language => language.EnglishName))
            {
                _allLanguages.Add(language);
            }

            _filteredLanguages = CollectionViewSource.GetDefaultView(_allLanguages);
            _filteredLanguages.Filter = FilterCultures;
            LanguagesList.ItemsSource = _filteredLanguages;
        }

        private bool FilterCultures(object obj)
        {
            if (obj is not CultureInfo culture)
            {
                return false;
            }

            bool isImplemented = _targetLanguageCodes.Contains(culture.Name);

            if (_showImplementedLanguagesOnly)
            {
                if (!isImplemented)
                {
                    return false;
                }
            }
            else
            {
                if (isImplemented)
                {
                    return false;
                }
            }

            string searchText = TxtSearch.Text;
            return string.IsNullOrWhiteSpace(searchText) || culture.EnglishName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                   culture.NativeName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                   culture.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filteredLanguages.Refresh();
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            ConfirmSelection();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void LstLanguages_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ConfirmSelection();
        }

        private void ConfirmSelection()
        {
            if (LanguagesList.SelectedItem is CultureInfo selectedCulture)
            {
                SelectedLanguageCode = selectedCulture.Name;
                DialogResult = true;
                Close();
            }
        }
    }
}
