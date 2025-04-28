using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ForgeTales.Converters
{
    public class AvatarFallbackConverter : IValueConverter
    {
        private static readonly BitmapImage DefaultAvatar =
            new BitmapImage(new Uri("pack://application:,,,/Images/default-avatar.png"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Если значение null или пустое - возвращаем аватар по умолчанию
                if (string.IsNullOrWhiteSpace(value?.ToString()))
                    return DefaultAvatar;

                string avatarData = value.ToString();

                // Если это base64 строка
                if (IsBase64String(avatarData))
                {
                    return ConvertBase64ToImage(avatarData) ?? DefaultAvatar;
                }

                // Если это путь к файлу
                if (File.Exists(avatarData))
                {
                    return new BitmapImage(new Uri(avatarData));
                }

                // Если это URI (например, pack:// или http://)
                if (Uri.TryCreate(avatarData, UriKind.RelativeOrAbsolute, out Uri uri))
                {
                    return new BitmapImage(uri);
                }
            }
            catch
            {
                // В случае любой ошибки возвращаем аватар по умолчанию
            }

            return DefaultAvatar;
        }

        private bool IsBase64String(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;

            try
            {
                System.Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private BitmapImage ConvertBase64ToImage(string base64String)
        {
            try
            {
                byte[] imageBytes = System.Convert.FromBase64String(base64String);
                var image = new BitmapImage();

                using (var ms = new MemoryStream(imageBytes))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze(); // Для безопасного использования в других потоках
                }

                return image;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Обратное преобразование не поддерживается");
        }
    }
}