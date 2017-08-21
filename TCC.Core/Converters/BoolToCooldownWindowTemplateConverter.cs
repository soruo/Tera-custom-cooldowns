﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace TCC.Converters
{
    public class BoolToCooldowWindowTemplateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return App.Current.FindResource("FixedCooldownTemplate");
            }
            else
            {
                return App.Current.FindResource("NormalCooldownTemplate");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
