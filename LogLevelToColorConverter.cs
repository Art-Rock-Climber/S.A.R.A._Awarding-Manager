using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace sara_coursework
{
    public class LogLevelToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string level)
            {
                switch (level)
                {
                    case "Error": return Brushes.LightCoral;
                    case "Warning": return Brushes.Khaki;
                    case "Info": return Brushes.LightGreen;
                    default: return Brushes.Transparent;
                }
            }
            return Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
