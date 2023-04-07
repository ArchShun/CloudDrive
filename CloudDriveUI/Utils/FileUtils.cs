using CloudDriveUI.Models;
using ImTools;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;

namespace CloudDrive.Utils;

public static class FileUtils
{
    /// <summary>
    /// Test a directory for create file access permissions
    /// </summary>
    /// <param name="DirectoryPath">Full path to directory </param>
    /// <param name="AccessRight">File System right tested</param>
    /// <returns>State [bool]</returns>
    public static bool DirectoryHasPermission(string DirectoryPath, FileSystemRights AccessRight)
    {
        if (string.IsNullOrEmpty(DirectoryPath)) return false;

        try
        {
            AuthorizationRuleCollection rules = new DirectoryInfo(DirectoryPath).GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier));
            WindowsIdentity identity = WindowsIdentity.GetCurrent();

            foreach (FileSystemAccessRule rule in rules)
            {
                if (identity.Groups?.Contains(rule.IdentityReference) ?? false)
                {
                    if ((AccessRight & rule.FileSystemRights) > 0)
                    {
                        if (rule.AccessControlType == AccessControlType.Allow)
                            return true;
                    }
                }
            }
        }
        catch { }
        return false;
    }
    public static readonly Dictionary<FileType, string[]> FileTypeDict = new()
    {
        {FileType.Video,new string[]{ ".avi",".mp4",".wmv",".asf",".asx",".rm",".rmvb",".3gp",".mov",".m4v",".dat",".mkv",".flv",".vob" } },
        {FileType.Audio,new string[]{ ".mp3",".wav",".wma",".mp2",".flac",".midi",".ra",".ape",".aac",".cda" } },
        {FileType.Picture,new string[]{".jpeg",".tiff",".raw",".bmp",".png",".gif",".jpg"} },
        {FileType.Document,new string[]{ ".doc",".txt",".docx",".xls",".xlsx",".ppt",".md"} },
        {FileType.Application,new string[]{".exe",".bat",".dll" } },
        {FileType.BitTorrent,new string[]{".torrent" } }
    };
    /// <summary>
    /// 计算文件大小
    /// </summary>
    /// <param name="bits">文件大小</param>
    /// <returns>转换单位后的文件大小</returns>
    public static string CalSize(long? bits)
    {
        if (bits == 0 || bits == null) return "0 B";
        var size = "";
        var units = new string[] { "B", "KB", "MB", "GB", "TB" };
        for (var i = 0; i < units.Length && bits > 0; i++)
        {
            size = bits.ToString() + " " + units[i];
            bits /= 1024;
        }
        return size;
    }

    /// <summary>
    /// 获取文件类型
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    public static FileType? GetFileType(FileInfo fileInfo)
    {
        return GetFileType(fileInfo.Extension);
    }

    /// <summary>
    /// 获取文件类型
    /// </summary>
    /// <param name="extension"></param>
    /// <returns></returns>
    public static FileType? GetFileType(string extension)
    {

        foreach (var k in FileTypeDict.Keys)
        {
            if (FileTypeDict[k].Contains(extension.ToLower()))
            {
                return k;
            }
        }
        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath1"></param>
    /// <param name="filePath2"></param>
    /// <returns></returns>
    public static bool CheckFileConsistency(string filePath1, string filePath2)
    {
        if (File.Exists(filePath1) && File.Exists(filePath2))
        {
            using (SHA256 mySHA256 = SHA256.Create())
            {
                var hash1 = GenerateFileSha256(mySHA256, filePath1);
                if (hash1 == null) return false;
                var hash2 = GenerateFileSha256(mySHA256, filePath2);
                if (hash2 == null) return false;
                return BitConverter.ToString(hash1).Equals(BitConverter.ToString(hash2));
            }
        }
        return false;
    }
    /// <summary>
    /// 根据指定文件生成哈希值
    /// </summary>
    /// <param name="sha256"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static byte[]? GenerateFileSha256(SHA256 sha256, string filePath)
    {
        byte[]? hashValue = null;
        using (FileStream fileStream = File.Open(filePath, FileMode.Open))
        {
            try
            {
                fileStream.Position = 0;
                hashValue = sha256.ComputeHash(fileStream);
            }
            catch (IOException e)
            {
                Console.WriteLine($"I/O Exception: {e.Message}");
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine($"Access Exception: {e.Message}");
            }
        }
        return hashValue;
    }

    /// <summary>
    /// 获取指定路径的本地文件
    /// </summary>
    public static void GetLocalFileInfos<T>(string path, ref List<T> result, Func<FileSystemInfo, T> converter, bool recursion = false)
    {
        var dirInfo = new DirectoryInfo(path);
        if (!dirInfo.Exists) return;
        foreach (var info in dirInfo.GetFileSystemInfos())
        {
            result.Add(converter(info));
            if (recursion && ((info!.Attributes & FileAttributes.Directory) > 0))
                GetLocalFileInfos(info.FullName, ref result, converter, true);
        }
    }

    /// <summary>
    /// 递归获取路径下的所有文件夹和文件
    /// </summary>
    /// <param name="path">根路径</param>
    /// <param name="recursion">是否递归</param>
    /// <returns></returns>
    public static IEnumerable<FileSystemInfo> GetAllFileInfos(string path, bool recursion = true)
    {
        var dirInfo = new DirectoryInfo(path);
        var result = new List<FileSystemInfo>();
        if (dirInfo.Exists)
        {
            foreach (var dir in dirInfo.GetDirectories())
            {
                result.Add(dir);
                result.AddRange(GetAllFileInfos(dir.FullName, recursion));
            }
            foreach(var file in dirInfo.GetFiles())
            {
                result.Add(file);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取本地文件节点
    /// </summary>
    /// <param name="path">根路径</param>
    /// <param name="recursion">是否递归</param>
    /// <param name="relocation">是否将根路径设置到 path </param>
    /// <returns></returns>
    public static Node<FileSystemInfo> GetLocalFileNode(string path, bool recursion = true, bool relocation = true)
    {
        var locInfos = GetAllFileInfos(path,recursion);
        var root = new Node<FileSystemInfo>("root");
        foreach (var itm in locInfos)
        {
            var node = new Node<FileSystemInfo>(itm.Name, itm);
            root.Insert(node, itm.FullName,Path.DirectorySeparatorChar);
        }
        if (relocation)
        {
            root = root.GetNode(path);
            root.Parent = null;
        }
        return root;
    }

}
