﻿#region

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

#endregion

namespace NextCanvas.Converters
{
    public class OrConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var item in values)
                if (item is bool b && b)
                    return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}