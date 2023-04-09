using CloudDrive.Utils;
using CloudDriveUI.Utils;
using System.Xml.Linq;

namespace CloudDriveUI.Models;

public class SyncFileItem : FileItemBase
{
    private readonly CloudFileInfo? cloudFileInfo;
    private readonly FileSystemInfo? fileSystemInfo;


    #region 构造方法

    /// <summary>
    /// 从远程文件创建文件列表项
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public SyncFileItem(CloudFileInfo info)
    {
        cloudFileInfo = info;
    }

    /// <summary>
    /// 从文件系统创建列表项
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public SyncFileItem(FileSystemInfo info)
    {
        fileSystemInfo = info;
    }

    /// <summary>
    /// 根据本地文件信息和远程文件信息创建文件列表项
    /// </summary>
    /// <param name="local"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    public SyncFileItem(FileSystemInfo local, CloudFileInfo remote)
    {
        cloudFileInfo = remote;
        fileSystemInfo = local;
    }

    #endregion


    #region 属性
    public override string Id { get; } = Guid.NewGuid().ToString();
    public override string Name => fileSystemInfo != null ? fileSystemInfo.Name : (cloudFileInfo?.Name ?? "");
    public override bool IsDir => cloudFileInfo?.IsDir ?? ((fileSystemInfo!.Attributes & FileAttributes.Directory) > 0);
    public override FileType FileType => IsDir ? FileType.Other : (cloudFileInfo?.Category ?? FileUtils.GetFileType((FileInfo)fileSystemInfo!));
    public override string Size => IsDir ? "--" : (FileUtils.CalSize(cloudFileInfo?.Size ?? ((FileInfo)fileSystemInfo!).Length));

    public DateTime? RemoteUpdate
    {
        get
        {
            if (cloudFileInfo?.LocalMtime != null)
                return DateTimeUtils.TimeSpanToDateTime((long)cloudFileInfo.LocalMtime);
            else return null;
        }
    }

    public DateTime? LocalUpdate => fileSystemInfo?.LastWriteTime;

    /// <summary>
    /// 同步状态
    /// </summary>
    public SynchState State
    {
        get
        {
            if (RemoteUpdate != null && LocalUpdate != null)
            {
                var dt = ((TimeSpan)(RemoteUpdate - LocalUpdate)).TotalSeconds;
                if (dt > 3) return SynchState.ToUpdate;
                else if (dt < -3) return SynchState.Modified;
                else return SynchState.Consistent;
            }
            else if (cloudFileInfo != null)
                return SynchState.ToUpdate;
            else
                return SynchState.Added;
        }
    }

    public string? LocalPath => fileSystemInfo?.FullName;
    public string? RemotePath => cloudFileInfo?.Path;

    /// <summary>
    /// 根据远程文件节点和本地文件节点创建文件列表项
    /// </summary>
    /// <param name="localNode"></param>
    /// <param name="remoteNode"></param>
    /// <returns></returns>
    public static IEnumerable<SyncFileItem> CreateItems(Node<FileSystemInfo> localNode, Node<CloudFileInfo> remoteNode)
    {
        var result = new List<SyncFileItem>();
        // 遍历远程文件，处理相同路径节点和仅在远程存在节点
        foreach (var itm in remoteNode)
        {
            // itm.Path.Split("\\")[2..]: Path返回的以 \ 开头的路径，TryGetValue 需要传入相对于当前节点的路径所以需要跳过两个路径（\和当前节点名）
            var res = localNode.TryGetValue(out FileSystemInfo? info, itm.Path.Split("\\")[2..]);
            if (res && info != null && itm.Value != null) result.Add(new SyncFileItem(info, itm.Value));
            else if (itm.Value != null) result.Add(new SyncFileItem(itm.Value));
        }
        // 遍历本地节点，处理仅在本地存在的节点
        foreach (var itm in localNode)
        {
            var res = remoteNode.TryGetValue(out _, itm.Path.Split("\\")[2..]);
            if (!res && itm.Value != null) result.Add(new SyncFileItem(itm.Value));
        }
        return result;
    }
    #endregion


}
