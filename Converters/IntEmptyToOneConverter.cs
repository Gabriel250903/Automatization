using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Automatization.Converters
{
    public class IntEmptyToOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? strValue = value?.ToString();
            return string.IsNullOrWhiteSpace(strValue) ? 1 : int.TryParse(strValue, out int intValue) ? intValue : DependencyProperty.UnsetValue;
        }
    }
}
