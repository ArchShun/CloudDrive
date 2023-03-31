using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDCloudDrive
{
    internal class FileUtils
    {

        public static string Path(string path)
        {
           return Regex.Replace(path, @"\\+", "/");
        }

        public static string Parent(string path)
        {
            var m = Regex.Match(path, @"^.+/");
            if (m.Success)
            {
                return m.Value;
            }
            return path;
        }
    }
}
