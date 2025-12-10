using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Brushes = System.Drawing.Brushes;

namespace Automatization.Converters
{
    public class HexToSolidColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor)
            {
                try
                {
                    if (!hexColor.StartsWith('#'))
                    {
                        hexColor = "#" + hexColor;
                    }
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom(hexColor) ?? Brushes.Gray);
                }
                catch
                {
                    return Brushes.Gray;
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
