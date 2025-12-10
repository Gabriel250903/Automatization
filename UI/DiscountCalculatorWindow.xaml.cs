using Automatization.Services;
using Automatization.Types;
using Automatization.Utils;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using ListBox = System.Windows.Controls.ListBox;

namespace Automatization.UI
{
    public partial class DiscountCalculatorWindow : FluentWindow, INotifyPropertyChanged
    {
        private readonly MarketService _marketService;
        private MarketItem? _selectedItem;
        private double _discountValue;
        private string _searchText = string.Empty;
        private int _selectedSortIndex = 0;
        private int _quantity = 1;
        private bool _isSupplyItemSelected = false;
        private MarketItem? _compareItemA;

        public ICollectionView TurretsView { get; private set; } = null!;
        public ICollectionView HullsView { get; private set; } = null!;
        public ICollectionView PaintsView { get; private set; } = null!;
        public ICollectionView SuppliesView { get; private set; } = null!;
        public ICollectionView ProductKitsView { get; private set; } = null!;
        public ICollectionView SuppliesKitsView { get; private set; } = null!;

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool IsComparisonMode => CompareToggle.IsChecked == true;
        private bool IsCumulativeMode => CumulativeToggle.IsChecked == true;

        public bool IsSupplyItemSelected
        {
            get => _isSupplyItemSelected;
            set
            {
                if (_isSupplyItemSelected != value)
                {
                    _isSupplyItemSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = Math.Max(1, Math.Min(99999, value));
                    OnPropertyChanged();
                    Recalculate();
                }
            }
        }

        public double DiscountValue
        {
            get => _discountValue;
            set
            {
                double clampedValue = Math.Max(0, Math.Min(100, value));
                if (Math.Abs(_discountValue - clampedValue) > 0.01)
                {
                    _discountValue = clampedValue;
                    OnPropertyChanged();
                    Recalculate();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    RefreshViews();
                }
            }
        }

        public int SelectedSortIndex
        {
            get => _selectedSortIndex;
            set
            {
                if (_selectedSortIndex != value)
                {
                    _selectedSortIndex = value;
                    OnPropertyChanged();
                    UpdateSorting();
                }
            }
        }

        public DiscountCalculatorWindow()
        {
            InitializeComponent();
            DataContext = this;
            _marketService = new MarketService();
            InitializeLists();
        }

        private void InitializeLists()
        {
            TurretsView = CollectionViewSource.GetDefaultView(_marketService.GetItemsByCategory(ItemCategory.Turret));
            HullsView = CollectionViewSource.GetDefaultView(_marketService.GetItemsByCategory(ItemCategory.Hull));
            PaintsView = CollectionViewSource.GetDefaultView(_marketService.GetItemsByCategory(ItemCategory.Paint));
            SuppliesView = CollectionViewSource.GetDefaultView(_marketService.GetItemsByCategory(ItemCategory.Supplies));
            ProductKitsView = CollectionViewSource.GetDefaultView(_marketService.GetItemsByCategory(ItemCategory.ProductKit));
            SuppliesKitsView = CollectionViewSource.GetDefaultView(_marketService.GetItemsByCategory(ItemCategory.SuppliesKit));

            ConfigureView(TurretsView);
            ConfigureView(HullsView);
            ConfigureView(PaintsView);
            ConfigureView(SuppliesView);
            ConfigureView(ProductKitsView);
            ConfigureView(SuppliesKitsView);

            UpdateSorting();
        }

        private void ConfigureView(ICollectionView view)
        {
            view.Filter = FilterMarketItem;
        }

        private bool FilterMarketItem(object obj)
        {
            return string.IsNullOrWhiteSpace(SearchText) || (obj is MarketItem item && item.LocalizedName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshViews()
        {
            TurretsView.Refresh();
            HullsView.Refresh();
            PaintsView.Refresh();
            SuppliesView.Refresh();
            ProductKitsView.Refresh();
            SuppliesKitsView.Refresh();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                if (!TurretsView.IsEmpty)
                {
                    TurretsExpander.IsExpanded = true;
                }

                if (!HullsView.IsEmpty)
                {
                    HullsExpander.IsExpanded = true;
                }

                if (!PaintsView.IsEmpty)
                {
                    PaintsExpander.IsExpanded = true;
                }

                if (!SuppliesView.IsEmpty)
                {
                    SuppliesExpander.IsExpanded = true;
                }

                if (!ProductKitsView.IsEmpty)
                {
                    ProductKitsExpander.IsExpanded = true;
                }

                if (!SuppliesKitsView.IsEmpty)
                {
                    SuppliesKitsExpander.IsExpanded = true;
                }
            }
        }

        private void UpdateSorting()
        {
            MarketItemComparer comparer = new(SelectedSortIndex);

            ApplySort(TurretsView, comparer);
            ApplySort(HullsView, comparer);
            ApplySort(PaintsView, comparer);
            ApplySort(SuppliesView, comparer);
            ApplySort(ProductKitsView, comparer);
            ApplySort(SuppliesKitsView, comparer);
        }

        private void ApplySort(ICollectionView view, IComparer comparer)
        {
            if (view is ListCollectionView listCollectionView)
            {
                listCollectionView.CustomSort = comparer;
            }
            else
            {
                view.SortDescriptions.Clear();
                switch (SelectedSortIndex)
                {
                    case 0: view.SortDescriptions.Add(new SortDescription("LocalizedName", ListSortDirection.Ascending)); break;
                    case 1: view.SortDescriptions.Add(new SortDescription("LocalizedName", ListSortDirection.Descending)); break;
                }
            }
        }

        private void ItemListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox sourceListBox && sourceListBox.SelectedItem is MarketItem item)
            {
                if (sourceListBox != TurretsListBox)
                {
                    TurretsListBox.SelectedIndex = -1;
                }

                if (sourceListBox != HullsListBox)
                {
                    HullsListBox.SelectedIndex = -1;
                }

                if (sourceListBox != PaintsListBox)
                {
                    PaintsListBox.SelectedIndex = -1;
                }

                if (sourceListBox != SuppliesListBox)
                {
                    SuppliesListBox.SelectedIndex = -1;
                }

                if (sourceListBox != ProductKitsListBox)
                {
                    ProductKitsListBox.SelectedIndex = -1;
                }

                if (sourceListBox != SuppliesKitsListBox)
                {
                    SuppliesKitsListBox.SelectedIndex = -1;
                }

                _selectedItem = item;
                UpdateDetailsUI(item);

                IsSupplyItemSelected = item.Category == ItemCategory.Supplies;
                if (IsSupplyItemSelected)
                {
                    Quantity = 1;
                }

                Recalculate();
            }
            else if (AreAllSelectionsEmpty())
            {
                ClearDetailsUI();
            }
        }

        private bool AreAllSelectionsEmpty()
        {
            return TurretsListBox.SelectedIndex == -1 &&
                   HullsListBox.SelectedIndex == -1 &&
                   PaintsListBox.SelectedIndex == -1 &&
                   SuppliesListBox.SelectedIndex == -1 &&
                   ProductKitsListBox.SelectedIndex == -1 &&
                   SuppliesKitsListBox.SelectedIndex == -1;
        }

        private void UpdateDetailsUI(MarketItem item)
        {
            SelectedItemText.Text = item.LocalizedName;

            if (!string.IsNullOrEmpty(item.ImageUrl))
            {
                try
                {
                    SelectedImage.Source = new BitmapImage(new Uri(item.ImageUrl));
                }
                catch
                {
                    SelectedImage.Source = null;
                }
            }
            else
            {
                SelectedImage.Source = null;
            }

            List<StatItem> stats = [];
            StringBuilder descriptionBuilder = new();

            string fullDescription = item.LocalizedDescription ?? string.Empty;
            string cleanFullDescription = new string([.. fullDescription.Where(c => !char.IsControl(c))]).Replace("&#x0a;", "");

            string? ranksLabel = Application.Current.TryFindResource("Label_Ranks") as string;
            string? discountLabel = Application.Current.TryFindResource("Label_Discount") as string;
            string? containsLabel = Application.Current.TryFindResource("Label_Contains") as string;
            string? hullLabel = Application.Current.TryFindResource("Label_Hull") as string;
            string? turretLabel = Application.Current.TryFindResource("Label_Turret") as string;
            string? paintLabel = Application.Current.TryFindResource("Label_Paint") as string;

            Dictionary<string, string> keywordMap = new()
            {
                { "Ranks:", ranksLabel ?? "Ranks" },
                { "Discount:", discountLabel ?? "Discount" },
                { "Contains:", containsLabel ?? "Contains" },
                { "Hull:", hullLabel ?? "Hull" },
                { "Turret:", turretLabel ?? "Turret" },
                { "Paint:", paintLabel ?? "Paint" }
            };

            List<(int Index, string Key)> foundKeywords = [];
            foreach (KeyValuePair<string, string> entry in keywordMap)
            {
                int index = cleanFullDescription.IndexOf(entry.Key, StringComparison.OrdinalIgnoreCase);
                if (index != -1)
                {
                    foundKeywords.Add((index, entry.Key));
                }
            }

            foundKeywords = [.. foundKeywords.OrderBy(k => k.Index)];

            if (foundKeywords.Count == 0 && !string.IsNullOrWhiteSpace(cleanFullDescription))
            {
                _ = descriptionBuilder.Append(item.LocalizedDescription);
            }
            else
            {
                if (foundKeywords.Count > 0 && foundKeywords[0].Index > 0)
                {
                    string preText = cleanFullDescription[..foundKeywords[0].Index].Trim();
                    if (!string.IsNullOrWhiteSpace(preText))
                    {
                        _ = descriptionBuilder.AppendLine(preText);
                    }
                }

                for (int i = 0; i < foundKeywords.Count; i++)
                {
                    (int Index, string Key) currentKeyword = foundKeywords[i];
                    int startIndex = currentKeyword.Index + currentKeyword.Key.Length;
                    int endIndex = (i + 1 < foundKeywords.Count) ? foundKeywords[i + 1].Index : cleanFullDescription.Length;

                    string key = currentKeyword.Key.TrimEnd(':');
                    string value = cleanFullDescription[startIndex..endIndex].Trim();

                    if (key.Trim() == "Ranks" || (!string.IsNullOrEmpty(ranksLabel) && key.Trim() == ranksLabel))
                    {
                        string[] separators = [" - ", " – ", " — "];
                        string[] rankParts = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                        if (rankParts.Length >= 2)
                        {
                            string rank1 = rankParts[0].Trim();
                            string rank2 = rankParts[1].Trim();

                            stats.Add(new StatItem
                            {
                                Key = key,
                                IsRank = true,
                                RankStartIcon = _marketService.GetRankIcon(rank1),
                                RankEndIcon = _marketService.GetRankIcon(rank2)
                            });
                        }
                        else
                        {
                            stats.Add(new StatItem { Key = key, Value = value });
                        }
                    }
                    else
                    {
                        stats.Add(new StatItem { Key = key, Value = value });
                    }
                }
            }

            SelectedDescriptionText.Text = descriptionBuilder.ToString().Trim();
            StatsControl.ItemsSource = stats;
        }

        private void ClearDetailsUI()
        {
            _selectedItem = null;
            SelectedItemText.Text = "Select an item";
            SelectedDescriptionText.Text = "";
            SelectedImage.Source = null;
            StatsControl.ItemsSource = null;
            ResultsList.ItemsSource = null;
            TotalCostText.Text = "0";
            TotalSavedText.Text = "0";
            IsSupplyItemSelected = false;
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                return;
            }

            if (sender is Expander expandedExpander)
            {
                if (expandedExpander != TurretsExpander && TurretsExpander != null)
                {
                    TurretsExpander.IsExpanded = false;
                }

                if (expandedExpander != HullsExpander && HullsExpander != null)
                {
                    HullsExpander.IsExpanded = false;
                }

                if (expandedExpander != PaintsExpander && PaintsExpander != null)
                {
                    PaintsExpander.IsExpanded = false;
                }

                if (expandedExpander != SuppliesExpander && SuppliesExpander != null)
                {
                    SuppliesExpander.IsExpanded = false;
                }

                if (expandedExpander != ProductKitsExpander && ProductKitsExpander != null)
                {
                    ProductKitsExpander.IsExpanded = false;
                }

                if (expandedExpander != SuppliesKitsExpander && SuppliesKitsExpander != null)
                {
                    SuppliesKitsExpander.IsExpanded = false;
                }
            }
        }

        private void CumulativeToggle_CheckedChanged(object sender, RoutedEventArgs e)
        {
            Recalculate();
        }

        private void CompareToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedItem != null)
            {
                _compareItemA = _selectedItem;
                SingleItemPanel.Visibility = Visibility.Collapsed;
                ComparisonHeaderPanel.Visibility = Visibility.Visible;
                ResultsList.Visibility = Visibility.Collapsed;
                ComparisonContainer.Visibility = Visibility.Visible;
                UpdateComparisonHeader();
                Recalculate();
            }
            else
            {
                CompareToggle.IsChecked = false;
            }
        }

        private void CompareToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _compareItemA = null;
            SingleItemPanel.Visibility = Visibility.Visible;
            ComparisonHeaderPanel.Visibility = Visibility.Collapsed;
            ResultsList.Visibility = Visibility.Visible;
            ComparisonContainer.Visibility = Visibility.Collapsed;
            Recalculate();
        }

        private void UpdateComparisonHeader()
        {
            if (_compareItemA != null)
            {
                CompareNameA.Text = _compareItemA.LocalizedName;
                if (!string.IsNullOrEmpty(_compareItemA.ImageUrl))
                {
                    CompareImageA.Source = new BitmapImage(new Uri(_compareItemA.ImageUrl));
                }
            }

            if (_selectedItem != null)
            {
                CompareNameB.Text = _selectedItem.LocalizedName;
                if (!string.IsNullOrEmpty(_selectedItem.ImageUrl))
                {
                    CompareImageB.Source = new BitmapImage(new Uri(_selectedItem.ImageUrl));
                }
            }
            else
            {
                CompareNameB.Text = "Select Item B";
                CompareImageB.Source = null;
            }
        }

        private void Recalculate()
        {
            int discountPercent = (int)DiscountValue;

            if (IsComparisonMode)
            {
                RecalculateComparison(discountPercent);
            }
            else
            {
                RecalculateSingle(discountPercent);
            }
        }

        private void RecalculateComparison(int discountPercent)
        {
            if (_compareItemA == null || _selectedItem == null)
            {
                return;
            }

            List<ComparisonPriceInfo> compareResults = [];
            int maxLevels = Math.Max(_compareItemA.Prices.Length, _selectedItem.Prices.Length);
            int cumulativeA = 0, cumulativeB = 0;

            for (int i = 0; i < maxLevels; i++)
            {
                string modificationText = GetModificationText(i, _compareItemA.Prices.Length == 1 && _selectedItem.Prices.Length == 1);

                int priceA = CalculatePrice(_compareItemA, i, discountPercent, ref cumulativeA);
                int priceB = CalculatePrice(_selectedItem, i, discountPercent, ref cumulativeB);

                compareResults.Add(new ComparisonPriceInfo
                {
                    Modification = modificationText,
                    PriceA = priceA,
                    PriceB = priceB,
                    Difference = priceB - priceA
                });
            }

            ComparisonListA.ItemsSource = compareResults;
            ComparisonListB.ItemsSource = compareResults;
            UpdateComparisonHeader();
        }

        private void RecalculateSingle(int discountPercent)
        {
            if (_selectedItem == null || ResultsList == null)
            {
                return;
            }

            List<ModificationPriceInfo> results = [];
            int totalCost = 0, totalSaved = 0;
            int cumulativeBase = 0, cumulativeSaved = 0, cumulativeFinal = 0;

            for (int i = 0; i < _selectedItem.Prices.Length; i++)
            {
                int basePrice = _selectedItem.Prices[i];
                int discountAmount = (int)(basePrice * (discountPercent / 100.0));
                int finalPrice = basePrice - discountAmount;

                if (_selectedItem.Category == ItemCategory.Supplies)
                {
                    basePrice *= Quantity;
                    discountAmount *= Quantity;
                    finalPrice *= Quantity;
                }

                totalCost += finalPrice;
                totalSaved += discountAmount;

                string modificationText = GetModificationText(i, _selectedItem.Prices.Length == 1);

                if (IsCumulativeMode)
                {
                    cumulativeBase += basePrice;
                    cumulativeSaved += discountAmount;
                    cumulativeFinal += finalPrice;

                    results.Add(new ModificationPriceInfo
                    {
                        Modification = modificationText,
                        BasePrice = cumulativeBase,
                        DiscountAmount = cumulativeSaved,
                        FinalPrice = cumulativeFinal
                    });
                }
                else
                {
                    results.Add(new ModificationPriceInfo
                    {
                        Modification = modificationText,
                        BasePrice = basePrice,
                        DiscountAmount = discountAmount,
                        FinalPrice = finalPrice
                    });
                }
            }

            ResultsList.ItemsSource = results;
            if (TotalCostText != null)
            {
                TotalCostText.Text = $"{totalCost:N0}";
            }

            if (TotalSavedText != null)
            {
                TotalSavedText.Text = $"{totalSaved:N0}";
            }
        }

        private string GetModificationText(int index, bool isSingle)
        {
            if (isSingle)
            {
                return "Standard";
            }

            if (IsCumulativeMode)
            {
                string text = "M0";
                for (int j = 1; j <= index; j++)
                {
                    text += $"+M{j}";
                }

                return text;
            }
            return $"M{index}";
        }

        private int CalculatePrice(MarketItem item, int index, int discountPercent, ref int cumulative)
        {
            int price = 0;
            if (index < item.Prices.Length)
            {
                int basePrice = item.Prices[index];
                int discount = (int)(basePrice * (discountPercent / 100.0));
                price = basePrice - discount;

                if (item.Category == ItemCategory.Supplies)
                {
                    price *= Quantity;
                }

                if (IsCumulativeMode)
                {
                    cumulative += price;
                    price = cumulative;
                }
            }
            return price;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void QuantityTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }
    }
}