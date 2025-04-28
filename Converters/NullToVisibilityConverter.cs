using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace ForgeTales.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string url && !string.IsNullOrEmpty(url))
                {
                    return new ImageBrush(new BitmapImage(new Uri(url, UriKind.RelativeOrAbsolute)))
                    {
                        Stretch = Stretch.UniformToFill
                    };
                }
            }
            catch
            {
                // В случае ошибки загрузки изображения
            }

            return new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Images/default-avatar.png")))
            {
                Stretch = Stretch.UniformToFill
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}