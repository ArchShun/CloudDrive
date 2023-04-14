using BDCloudDrive.Entities;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Reflection;

namespace BDCloudDrive;

public static class DataConvert
{
    private static readonly Dictionary<int, FileType> FileTypeDict = new()
    {
        { 1,FileType.Video },{2,FileType.Audio},{3,FileType.Picture},{4,FileType.Document},{5,FileType.Application},{6,FileType.Unknown},{7,FileType.BitTorrent}
    };

    public static FileType? ToFileType(int bd_category)
    {
        if (FileTypeDict.TryGetValue(bd_category, out var fileType)) return fileType;
        return null;
    }
    public static CloudFileInfo ToCloudFileInfo(CreateResult result)
    {
        var info = new CloudFileInfo()
        {
            Path = (PathInfo)result.Path,
            Name = result.Server_Filename,
            IsDir = result.Isdir == 1,
            ServerCtime = result.Ctime,
            ServerMtime = result.Mtime,
            Id = result.Fs_Id,
            Category = FileTypeDict[result.Category],
            LocalCtime = null,
            LocalMtime = null,
            Size = result.Size,
        };
        info.XData.Add("Md5", result.Md5);
        info.XData.Add("Name", result.Name);
        return info;
    }

    /// <summary>
    /// 同名属性映射，忽略大小写，忽略 _
    /// </summary>
    /// <param name="result"></param>
    /// <param name="nameMapper">名称映射，传入参数属性名:目标类型属性名</param>
    /// <param name="converts">手动进行数据类型转换，传入参数属性名:Func</param>
    /// <returns></returns>
    public static void ObjectToCloudFileInfo<T>(T result, ref CloudFileInfo info, Dictionary<string, string>? nameMapper = null, Dictionary<string, Func<T, object?>>? converts = null)
    {
        // 忽略属性名大小写
        var res_prop_kv = result?.GetType().GetProperties().ToDictionary(p => p.Name.ToLower().Replace("_", ""));
        var info_prop_kv = info.GetType().GetProperties().ToDictionary(p => p.Name.ToLower().Replace("_", ""));
        if (res_prop_kv != null && info_prop_kv != null)
        {
            foreach (var kv in res_prop_kv)
            {
                var pname = nameMapper?.TryGetValue(kv.Value.Name, out var name) ?? false ? name.ToLower().Replace("_", "") : kv.Key;
                if (info_prop_kv.TryGetValue(pname, out var infoProp))
                {
                    object? val = null;
                    // 如果有自定义类型转换就直接转换
                    if (converts?.TryGetValue(kv.Value.Name, out var func) ?? false)
                        val = func.Invoke(result);
                    // 检查类型能否兼容
                    else if (infoProp.PropertyType.IsAssignableFrom(kv.Value.PropertyType))
                        val = kv.Value.GetValue(result);
                    // 否则强制类型转换
                    else try
                        {
                            Type property_type = infoProp.PropertyType;
                            val = Convert.ChangeType(kv.Value.GetValue(result), property_type);
                        }
                        catch { }
                    if (val == null)
                        info.XData.Add(kv.Value.Name, kv.Value.GetValue(result));
                    else
                        infoProp.SetValue(info, val);
                }
            }
        }
    }
    public static CloudFileInfo ToCloudFileInfo(FileInfoResult result)
    {
        var info = new CloudFileInfo();
        var namemapper = new Dictionary<string, string>();
        namemapper.Add(nameof(result.Fs_Id), nameof(info.Id));
        namemapper.Add(nameof(result.Server_Filename), nameof(info.Name));
        var converts = new Dictionary<string, Func<FileInfoResult, object?>>();
        converts.Add(nameof(result.Category), res => ToFileType(res.Category));
        converts.Add(nameof(result.Path), res => new PathInfo(res.Path));
        ObjectToCloudFileInfo(result, ref info, namemapper, converts);
        return info;
    }
}
