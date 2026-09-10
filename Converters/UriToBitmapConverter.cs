using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Automatization.Services;

namespace Automatization.Converters
{
    public class UriToBitmapConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    string? localPath = ImageCacheService.GetCachedImagePathNonBlocking(
                        url,
                        out string fullLocalPath
                    );

                    BitmapImage bitmap = new();
                    bitmap.BeginInit();
                    bitmap.DecodePixelWidth = 100;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

                    if (localPath != null)
                    {
                        bitmap.UriSource = new Uri(localPath);
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }
                    else
                    {
                        bitmap.UriSource = new Uri(url);
                        bitmap.EndInit();
                        if (bitmap.CanFreeze)
                        {
                            bitmap.Freeze();
                        }
                    }

                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }

            return null;
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
