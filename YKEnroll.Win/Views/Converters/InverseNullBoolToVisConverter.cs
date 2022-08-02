using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace YKEnroll.Win.Views.Converters
{
    public sealed class InverseNullBoolToVisConverter : IValueConverter
    {        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {            
            if (value is Nullable<bool>)
            {
                Nullable<bool> tmp = (Nullable<bool>)value;
                if(tmp == true)
                {
                    return Visibility.Collapsed;
                }
                else if (tmp.HasValue && tmp.Value == false)
                {
                    return Visibility.Visible;
                }               
            }
            return DependencyProperty.UnsetValue;
        }
       
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
