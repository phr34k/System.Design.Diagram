using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace System.Design.Diagram
{
    public static class GraphNodeCommands
    {       

        public static RoutedUICommand SelectTexture
        {
            get;
            private set;
        }

        public static RoutedUICommand SelectCode
        {
            get;
            private set;
        }

        public static RoutedUICommand SelectColor
        {
            get;
            private set;
        }

        static GraphNodeCommands()
        {
            SelectTexture = new RoutedUICommand("Displays a UIEditor for selecting textures", "SelectTexture", typeof(GraphNodeCommands));
            SelectCode = new RoutedUICommand("Displays a UIEditor for defining code", "SelectCode", typeof(GraphNodeCommands));
            SelectColor = new RoutedUICommand("Displays a UIEditor for defining color", "SelectColor", typeof(GraphNodeCommands));            
        }

    }


    public static class ApplicationExtCommands
    {
        public static RoutedUICommand Build
        {
            get;
            private set;
        }

        static ApplicationExtCommands()
        {
            Build = new RoutedUICommand("Compiles a shader from the specified RootNode", "Build", typeof(ApplicationExtCommands));
        }
    }
}
