using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ForgeTales.Converters
{
    public class CoverImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string imageData = value as string;

                // Если данные не переданы или пустые - возвращаем стандартную обложку
                if (string.IsNullOrWhiteSpace(imageData))
                    return GetDefaultImage();

                // Если это base64 строка
                if (IsBase64String(imageData))
                {
                    byte[] imageBytes = System.Convert.FromBase64String(imageData);
                    var image = new BitmapImage();

                    using (var ms = new MemoryStream(imageBytes))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                    }

                    return image;
                }

                // Если это путь к файлу (например, стандартная обложка)
                if (imageData.StartsWith("pack://") || File.Exists(imageData))
                {
                    return new BitmapImage(new Uri(imageData, UriKind.RelativeOrAbsolute));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка конвертации изображения: {ex.Message}");
            }

            return GetDefaultImage();
        }

        private bool IsBase64String(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;

            try
            {
                // Используем полное имя класса System.Convert
                System.Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private BitmapImage GetDefaultImage()
        {
            try
            {
                return new BitmapImage(new Uri("pack://application:,,,/Images/back.jpg", UriKind.RelativeOrAbsolute));
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}