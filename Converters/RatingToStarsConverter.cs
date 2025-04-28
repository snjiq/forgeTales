using System;
using System.Globalization;
using System.Windows.Data;
using System.Text;

namespace ForgeTales.Converters
{
    public class RatingToStarsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int rating)
            {
                StringBuilder stars = new StringBuilder();

                // Добавляем заполненные звезды
                for (int i = 0; i < rating; i++)
                {
                    stars.Append("★"); // U+2605 BLACK STAR
                }

                // Добавляем пустые звезды
                for (int i = rating; i < 5; i++)
                {
                    stars.Append("☆"); // U+2606 WHITE STAR
                }

                return stars.ToString();
            }
            return "☆☆☆☆☆";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}