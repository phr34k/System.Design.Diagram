using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Globalization;
using System.Design.Diagram.Nodes;

namespace System.Design.Diagram
{

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                              CultureInfo culture)
        {
            if (value is Boolean)
            {
                return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                                  CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GraphNodeItemContainerStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            object[] obj = item.GetType().GetCustomAttributes(typeof(GraphNodeCategoryAttribute), false);
            string category = obj.Length > 0 ? (obj[0] as GraphNodeCategoryAttribute).Name : "Default";

            switch (category)
            {
                case "Parameters":
                    return (FindAnchestor<DesignerCanvas>(container) as DesignerCanvas).Resources["ParameterGroupStyle"] as Style;
                case "Root":
                    return (FindAnchestor<DesignerCanvas>(container) as DesignerCanvas).Resources["RootGroupStyle"] as Style;                   
                default:
                    return (FindAnchestor<DesignerCanvas>(container) as DesignerCanvas).Resources["DefaultStyle"] as Style;
            }


        }

        // Helper to search up the VisualTree
        private static T FindAnchestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
    }

    public class GraphNodeNameConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {

                object[] obj1 = (value as Type).GetCustomAttributes(typeof(GraphNodeCategoryAttribute), false);
                object[] obj2 = (value as Type).GetCustomAttributes(typeof(GraphNodeFriendlyNameAttribute), false);
                string category = obj1.Length > 0 ? (obj1[0] as GraphNodeCategoryAttribute).Name : string.Empty;
                string classname = obj2.Length > 0 ? (obj2[0] as GraphNodeFriendlyNameAttribute).Name : (value as Type).Name;

                if (category.Length == 0)
                    return classname;
                else
                    return string.Format("{0}.{1}", category, classname);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
