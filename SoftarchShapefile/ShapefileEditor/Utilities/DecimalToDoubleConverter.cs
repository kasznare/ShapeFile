using System;
using System.Windows.Data;

namespace ShapefileEditor
{
    public class DecimalToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            return (decimal)(double)value;
            if (value is int)
                return (decimal)(int)value;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is decimal)
            {
                //if (targetType == typeof(Int32))
                //    return (int)(decimal)value;
                //if (targetType == typeof(Double))
                    return (double)(decimal)value;
                //return value;
            }
            return value;
        }
    }
}
