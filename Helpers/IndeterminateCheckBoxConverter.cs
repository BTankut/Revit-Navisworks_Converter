using System;
using System.Globalization;
using System.Windows.Data;

namespace RvtToNavisConverter.Helpers
{
    public class IndeterminateCheckBoxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Direkt nullable bool değerini döndür
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Direkt değeri döndür
            return value;
        }
    }
}