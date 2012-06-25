using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace System.Design.Diagram
{
    class PathHelper
    {

        public static string PathFromProcess(string path)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), path);
        }

        public static string PathRelativeToProcess(string path)
        {
            return path.Replace(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "");
        }
    }
}
