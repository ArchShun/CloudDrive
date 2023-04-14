using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDCloudDrive
{
    internal class FileUtils
    {

        /// <summary>
        /// 百度网盘路径
        /// </summary>
        /// <param name="path">以 / 开头，末尾没有 / </param>
        /// <returns></returns>
        public static string Path(string path)
        {
            if (string.IsNullOrEmpty(path) || path == ".") return "/";
            return '/' + path.Replace("\\", "/").Trim('/');
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
