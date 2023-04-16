using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CloudDrive.Entities;

public class PathInfo
{
    private string _data = "";
    private static readonly Regex _separatorRegex = new(@"[\\/]+");
    private bool _readonly = false;


    public char Separator { get; private set; } = Path.DirectorySeparatorChar;

    #region 构造方法
    public PathInfo() { }
    public PathInfo(string path)
    {
        _data = path;
    }

    public PathInfo(IEnumerable<string> paths)
    {
        _data = string.Join(Separator, paths);
    }
    #endregion

    public PathInfo Lock()
    {
        _readonly = true;
        return this;
    }


    /// <summary>
    /// 获取全路径名
    /// </summary>
    /// <param name="startWithSeparator"></param>
    /// <param name="endWithSeparator"></param>
    /// <returns></returns>
    public string GetFullPath(bool startWithSeparator = false, bool endWithSeparator = false, char? separator = null)
    {
        separator ??= Separator;
        var res = _separatorRegex.Replace(_data, separator.ToString()!).Trim((char)separator);
        if (startWithSeparator) res = separator + res;
        if (endWithSeparator) res += separator;
        return res;
    }
    /// <summary>
    /// 获取名字
    /// </summary>
    /// <returns></returns>
    public string GetName(bool withExt = true)
    {
        var name = GetFullPath().Split(Separator)[^1] ?? "";
        return withExt ? name : name.Split('.')[0];
    }
    /// <summary>
    /// 获取父路径
    /// </summary>
    /// <param name="startWithSeparator"></param>
    /// <param name="endWithSeparator"></param>
    /// <returns></returns>
    public string GetParentPath(bool startWithSeparator = false, bool endWithSeparator = false, char? separator = null)
    {
        separator ??= Separator;
        var res = string.Join((char)separator, GetFullPath(separator: separator).Split('\\', '/')[..^1]);
        if (startWithSeparator) res = separator + res;
        if (endWithSeparator) res += separator;
        return res;
    }

    /// <summary>
    /// 获取扩展名，包含 . 符号
    /// </summary>
    /// <returns></returns>
    public string GetExtension()
    {
        var name = GetName();
        return name.Contains('.') ? "." + name.Split('.')[^1] : "";
    }

    /// <summary>
    /// 创建副本
    /// </summary>
    /// <returns></returns>
    public PathInfo Duplicate()
    {
        return new PathInfo(_data);
    }
    /// <summary>
    /// 拼接路径
    /// </summary>
    /// <param name="path">拼接路径</param>
    /// <param name="duplicate">返回副本，不修改原对象</param>
    /// <returns></returns>
    public PathInfo Join(string path, bool duplicate = true)
    {
        if (duplicate) return new PathInfo(_data + Separator + path);
        else if (_readonly) throw new InvalidOperationException();
        else _data += Separator + path;
        return this;
    }
    /// <summary>
    /// 拼接路径
    /// </summary>
    /// <param name="path">拼接路径</param>
    /// <param name="duplicate">返回副本，不修改原对象</param>
    /// <returns></returns>
    public PathInfo Join(PathInfo path, bool duplicate = true)
    {
        return Join(path.GetFullPath(), duplicate);
    }
    /// <summary>
    /// 拼接路径
    /// </summary>
    /// <param name="paths">拼接路径</param>
    /// <param name="duplicate">返回副本，不修改原对象</param>
    /// <returns></returns>
    public PathInfo Join(IEnumerable<string> paths, bool duplicate = true)
    {
        return Join(string.Join(Separator, paths), duplicate);
    }
    /// <summary>
    /// 获取相对路径
    /// </summary>
    /// <param name="_base"></param>
    /// <returns></returns>
    public PathInfo GetRelative(PathInfo basePath)
    {
        var arr1 = GetFullPath(separator: Separator).Split(Separator);
        var arr2 = basePath.GetFullPath(separator: Separator).Split(Separator);
        int i = 0;
        while (i < arr1.Length && i < arr2.Length && arr1[i] == arr2[i]) i++;
        return new PathInfo(arr1[i..]);
    }

    public PathInfo GetRelative(string basePath)
    {
        return GetRelative(new PathInfo(basePath));
    }

    public static explicit operator PathInfo(string str)
    {
        return new PathInfo(str);
    }

    public static explicit operator string(PathInfo info)
    {
        return info.GetFullPath();
    }

}
