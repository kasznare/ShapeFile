using System;
using System.Windows.Data;
using NetTopologySuite.IO;

namespace ShapefileEditor
{
    public class FieldMinMaxValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(Decimal))
            {
                if (value == null || parameter == null)
                    return null;
                if (value is DbaseFieldDescriptor)
                    if (parameter is string)
                    {
                        DbaseFieldDescriptor descriptor = (DbaseFieldDescriptor)value;
                        string param = ((string)parameter).ToLower();
                        if (param == "min")
                            return (decimal)((1 - Math.Pow(10, descriptor.Length - 1)) * Math.Pow(0.1, descriptor.DecimalCount));
                        else if (param == "max")
                            return (decimal)((Math.Pow(10, descriptor.Length) - 1) * Math.Pow(0.1, descriptor.DecimalCount));
                    }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }
}
