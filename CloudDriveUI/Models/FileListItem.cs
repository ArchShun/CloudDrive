using BDCloudDrive.Utils;
using CloudDrive.Utils;
using System.Runtime.CompilerServices;
using System.Xml;

namespace CloudDriveUI.Models;

/// <summary>
/// 文件列表显示项目
/// </summary>
public record FileListItem
{
    #region 属性
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;

    public DateTime? RemoteUpdate { get; set; }
    public DateTime LocalUpdate { get; set; } = DateTime.Now;
    public long Size { get; set; }
    public FileType? FileType { get; set; }
    public bool IsDir { get; set; } = false;
    public SynchState State { get; set; } = SynchState.Detached;
    public string? LocalPath { get; set; }
    public string? RemotePath { get; set; }
    #endregion

    public FileListItem() { }


    /// <summary>
    /// 从远程文件创建文件列表项
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public FileListItem(CloudFileInfo info)
    {
        Id = info.Id.ToString();
        RemoteUpdate = DateTimeUtils.TimeSpanToDateTime(info.LocalMtime ?? info.ServerMtime);
        IsDir = info.IsDir;
        RemotePath = info.Path;
        Name = info.Name;
        FileType = info.Category;
        Size = info.Size;
        State = SynchState.ToUpdate;
    }

    /// <summary>
    /// 从文件系统创建列表项
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public FileListItem(FileSystemInfo info)
    {
        IsDir = (info!.Attributes & FileAttributes.Directory) > 0;
        Name = info.Name;
        LocalUpdate = info.LastWriteTime;
        LocalPath = info.FullName;
        FileType = IsDir ? null : FileUtils.GetFileType((FileInfo)info);
        Size = IsDir ? -1 : ((FileInfo)info).Length;
        State = SynchState.Added;
    }

    /// <summary>
    /// 根据本地文件信息和远程文件信息创建文件列表项
    /// </summary>
    /// <param name="local"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    public FileListItem(FileSystemInfo local, CloudFileInfo remote)
    {
        Id = remote.Id.ToString();
        Name = remote.Name;
        IsDir = remote.IsDir;
        FileType = remote.Category;
        Size = remote.Size;
        RemoteUpdate = DateTimeUtils.TimeSpanToDateTime(remote.LocalMtime ?? remote.ServerMtime);
        RemotePath = remote.Path;
        LocalPath = local.FullName;
        LocalUpdate = local.LastWriteTime;
        var dt = ((DateTime)RemoteUpdate - LocalUpdate).TotalSeconds;
        if (dt > 3) State = SynchState.ToUpdate;
        else if (dt < -3) State = SynchState.Modified;
        else State = SynchState.Consistent;
    }

    //public static IEnumerable<FileListItem> CreateItems(IEnumerable<FileSystemInfo> local, IEnumerable<CloudFileInfo> remote,string relative_path_local,string relative_path_remote)
    //{
    //    var loc_dict = new Dictionary<string, FileSystemInfo>();
    //    var rem_dict = new Dictionary<string, CloudFileInfo>();
    //    foreach (var info in local)
    //    {
    //        var isDir = (info!.Attributes & FileAttributes.Directory) > 0;
    //        var k = $"{isDir}-{Path.GetRelativePath(relative_path_local, info.FullName)}";
    //        loc_dict.Add(k, info);
    //    }
    //    foreach (var info in res)
    //    {
    //        var k = $"{info.IsDir}-{Path.GetRelativePath(rem_path, info.Path)}";
    //        rem.Add(k, info);
    //    }
    //    // 遍历本地文件，如果远程也存在则根据本地和远程创建列表项
    //    foreach (var kv in loc_dict)
    //    {
    //        if (rem_dict.ContainsKey(kv.Key)) result.Add(new FileListItem(kv.Value, rem_dict[kv.Key]));
    //        else result.Add(new FileListItem(kv.Value));
    //    }
    //    // 遍历远程文件，如果本地不存在，则创建列表项
    //    foreach (var kv in rem_dict)
    //    {
    //        if (!loc_dict.ContainsKey(kv.Key)) result.Add(new FileListItem(kv.Value));
    //    }
    //}


}