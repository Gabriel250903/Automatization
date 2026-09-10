using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Automatization.Converters
{
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long numValue = 0;
            bool isNumeric = false;

            if (value is int i)
            {
                numValue = i;
                isNumeric = true;
            }
            else if (value is long l)
            {
                numValue = l;
                isNumeric = true;
            }

            if (isNumeric)
            {
                bool isVisible = numValue > 0;

                if (
                    parameter is string paramString
                    && paramString.Equals("Inverse", StringComparison.OrdinalIgnoreCase)
                )
                {
                    isVisible = !isVisible;
                }

                return isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            throw new NotImplementedException();
        }
    }
}
