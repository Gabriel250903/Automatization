using Automatization.Types;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Automatization.Converters
{
    public class CategoryToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ItemCategory category && category == ItemCategory.Supplies ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
