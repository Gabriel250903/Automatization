using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Automatization.Converters
{
    public class DoubleEmptyToZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? strValue = value?.ToString();
            return string.IsNullOrWhiteSpace(strValue)
                ? 0.0
                : double.TryParse(strValue, out double doubleValue) ? doubleValue : DependencyProperty.UnsetValue;
        }
    }
}