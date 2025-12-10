using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Automatization.Converters
{
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                bool isVisible = intValue > 0;

                if (parameter is string paramString && paramString.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    isVisible = !isVisible;
                }

                return isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
