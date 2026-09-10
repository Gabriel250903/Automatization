using System.Globalization;
using System.Windows.Data;
using Brushes = System.Windows.Media.Brushes;

namespace Automatization.Converters
{
    public class IntToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long val = 0;
            bool isNumeric = false;

            if (value is int i)
            {
                val = i;
                isNumeric = true;
            }
            else if (value is long l)
            {
                val = l;
                isNumeric = true;
            }

            if (isNumeric)
            {
                if (val < 0)
                {
                    return Brushes.LightGreen;
                }

                if (val > 0)
                {
                    return Brushes.IndianRed;
                }
            }
            return Brushes.Gray;
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
