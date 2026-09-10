using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;

namespace Automatization.Converters
{
    public class HexToSolidColorBrushConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, SolidColorBrush> _brushCache = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor)
            {
                if (!hexColor.StartsWith('#'))
                {
                    hexColor = "#" + hexColor;
                }

                if (_brushCache.TryGetValue(hexColor, out SolidColorBrush? cachedBrush))
                {
                    return cachedBrush;
                }

                try
                {
                    if (new BrushConverter().ConvertFrom(hexColor) is SolidColorBrush brush)
                    {
                        if (brush.CanFreeze)
                        {
                            brush.Freeze();
                        }
                        _ = _brushCache.TryAdd(hexColor, brush);
                        return brush;
                    }
                }
                catch { }
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
