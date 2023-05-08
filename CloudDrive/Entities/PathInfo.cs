using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Path = System.IO.Path;

namespace CloudDrive.Entities;

public class PathInfo
{

    private List<string> _data = new();
    private static readonly Regex _separatorRegex = new(@"[\\/]+");
    private bool _locked = false;
    private readonly char _separator = Path.DirectorySeparatorChar;

    #region 构造方法
    public PathInfo() { }
    public PathInfo(string path)
    {
        _data = _separatorRegex.Split(path).Where(e => !string.IsNullOrEmpty(e)).ToList();
    }

    public PathInfo(IEnumerable<string> paths)
    {
        _data = paths.SelectMany(path => _separatorRegex.Split(path).Where(e => !string.IsNullOrEmpty(e))).ToList();
    }
    #endregion


    public PathInfo Lock()
    {
        _locked = true;
        return this;
    }

    public string[] GetSegmentPath()
    {
        return _data.ToArray();
    }

    /// <summary>
    /// 获取全路径名
    /// </summary>
    /// <param name="startWithSeparator"></param>
    /// <param name="endWithSeparator"></param>
    /// <returns></returns>
    public string GetFullPath(bool startWithSeparator = false, bool endWithSeparator = false, char? separator = null)
    {
        separator ??= _separator;
        var res = string.Join((char)separator, _data);
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
        if (_data.Count == 0) return "";
        string name = _data[^1];
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
        separator ??= _separator;
        var res = string.Join((char)separator, _data.Take(_data.Count - 1));
        if (startWithSeparator) res = separator + res;
        if (endWithSeparator) res += separator;
        return res;
    }
    public PathInfo? GetParent()
    {
        if (_data.Count == 0) return null;
        var ret = Duplicate();
        ret._data = _data.Take(_data.Count - 1).ToList();
        return ret;
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
        return new PathInfo() { _data = _data.ToList() };
    }
    /// <summary>
    /// 拼接路径
    /// </summary>
    /// <param name="path">拼接路径</param>
    /// <param name="duplicate">返回副本，不修改原对象</param>
    /// <returns></returns>
    public PathInfo Join(string path, bool duplicate = true)
    {
        return Join(new PathInfo(path), duplicate);
    }
    /// <summary>
    /// 拼接路径
    /// </summary>
    /// <param name="path">拼接路径</param>
    /// <param name="duplicate">返回副本，不修改原对象</param>
    /// <returns></returns>
    public PathInfo Join(PathInfo path, bool duplicate = true)
    {
        if (duplicate)
        {
            var tmp = Duplicate();
            tmp._data.AddRange(path._data);
            return tmp;
        }
        else if (_locked) throw new InvalidOperationException("路径已锁定");
        else _data.AddRange(path._data);
        return this;
    }
    /// <summary>
    /// 拼接路径
    /// </summary>
    /// <param name="paths">拼接路径</param>
    /// <param name="duplicate">返回副本，不修改原对象</param>
    /// <returns></returns>
    public PathInfo Join(IEnumerable<string> paths, bool duplicate = true)
    {
        return Join(new PathInfo(paths), duplicate);
    }
    /// <summary>
    /// 获取相对路径
    /// </summary>
    /// <param name="_base"></param>
    /// <returns></returns>
    public PathInfo GetRelative(PathInfo basePath)
    {
        var _base_data = basePath._data;
        int i = 0;
        while (i < _data.Count && i < _base_data.Count && _data[i] == _base_data[i]) i++;
        return new PathInfo() { _data = _data.Skip(i).ToList() };
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

    public override string? ToString()
    {
        return GetFullPath();
    }
}
