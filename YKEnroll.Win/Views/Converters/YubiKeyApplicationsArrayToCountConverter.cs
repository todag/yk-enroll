using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace YKEnroll.Win.Views.Converters
{
    public sealed class YubiKeyApplicationsArrayToCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is string[])
            {
                string[] apps = (string[])value;
                if (apps.Count() == 0)
                    return 0;
                else if (apps.Count() == 1 && apps[0] == "None")
                    return 0;
                else
                    return apps.Count();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
