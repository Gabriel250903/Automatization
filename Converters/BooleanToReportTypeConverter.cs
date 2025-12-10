using System.Globalization;
using System.Windows.Data;

namespace Automatization.Converters
{
    public class BooleanToReportTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isIdea ? isIdea ? "Idea" : "Issue" : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
