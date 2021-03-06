﻿using System.Windows;
using System.Windows.Media;

namespace NextCanvas.Utilities
{
    public static class WpfTreeUtilities
    {
        public static T FindVisualChild<T>(DependencyObject obj)
            where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T variable)
                    return variable;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild == null) continue;
                return childOfChild;
            }
            return null;
        }

        public static T FindLogicalParent<T>(DependencyObject d) where T : DependencyObject
        {
            while (!(d is T))
            {
                d = LogicalTreeHelper.GetParent(d);
                if (d == null)
                {
                    return null;
                }
            }
            return d as T;
        }
    }
}
