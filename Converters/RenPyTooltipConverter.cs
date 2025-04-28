using System.Globalization;
using System;
using System.Windows.Data;

namespace ForgeTales.Converters
{
    public class RenPyTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool isRenPy && isRenPy)
                ? "Запустить проект Ren'Py"
                : "Читать текстовую главу";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}