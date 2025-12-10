using System.Globalization;
using System.Windows.Data;
using Brushes = System.Windows.Media.Brushes;

namespace Automatization.Converters
{
    public class IntToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                if (intValue < 0)
                {
                    return Brushes.LightGreen;
                }

                if (intValue > 0)
                {
                    return Brushes.IndianRed;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
