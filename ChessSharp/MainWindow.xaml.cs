using System;
using System.Globalization;
using System.Windows.Data;

namespace ChessSharp
{
    public class MinValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter is string heightString && double.TryParse(heightString, out double height))
            {
                return Math.Min(width, height);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
