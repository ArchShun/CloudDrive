using CloudDrive.Utils;
using CloudDriveUI.Utils;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CloudDriveUI.Models;

public class SynchFileItem : FileItemBase
{
    private readonly CloudFileInfo? remoteInfo;
    private readonly FileSystemInfo? localInfo;

    #region 构造方法

    /// <summary>
    /// 从远程文件创建文件列表项
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    private SynchFileItem(CloudFileInfo info)
    {
        remoteInfo = info;
    }

    /// <summary>
    /// 从文件系统创建列表项
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    private SynchFileItem(FileSystemInfo info)
    {
        localInfo = info;
    }

    /// <summary>
    /// 根据本地文件信息和远程文件信息创建文件列表项
    /// </summary>
    /// <param name="local"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    private SynchFileItem(FileSystemInfo local, CloudFileInfo remote)
    {
        remoteInfo = remote;
        localInfo = local;
    }

    #endregion

    #region 属性
    public override string Id { get; } = Guid.NewGuid().ToString();
    public override string Name => localInfo != null ? localInfo.Name : (remoteInfo?.Name ?? "");
    public override bool IsDir => remoteInfo?.IsDir ?? ((localInfo!.Attributes & FileAttributes.Directory) > 0);
    public override FileType FileType => IsDir ? FileType.Unknown : (remoteInfo?.Category ?? FileUtils.GetFileType((FileInfo)localInfo!));
    public override string Size => IsDir ? "--" : (FileUtils.CalSize(remoteInfo?.Size ?? ((FileInfo)localInfo!).Length));

    public DateTime? RemoteUpdate
    {
        get
        {
            if (remoteInfo?.LocalMtime != null)
                return DateTimeUtils.TimeSpanToDateTime(remoteInfo.LocalMtime ?? remoteInfo.ServerMtime);
            else return null;
        }
    }

    public DateTime? LocalUpdate => localInfo?.LastWriteTime;

    /// <summary>
    /// 同步状态, 文件夹节点的状态由子节点状态决定
    /// </summary>
    public SynchState State { get; private set; } = SynchState.Unknown;

    public PathInfo? LocalPath => localInfo != null ? (PathInfo)localInfo.FullName : null;
    public PathInfo? RemotePath => remoteInfo?.Path;

    #endregion

    /// <summary>
    /// 根据远程文件节点和本地文件节点创建树状文件列表项
    /// </summary>
    /// <param name="localNode"></param>
    /// <param name="remoteNode"></param>
    /// <param name="rootName"></param>
    /// <returns></returns>
    public static Node<SynchFileItem> CreateItemNode(Node<FileSystemInfo> localNode, Node<CloudFileInfo> remoteNode, string rootName = "root")
    {
        var result = new Node<SynchFileItem>(rootName);
        // 遍历远程文件，处理相同路径节点和仅在远程存在节点
        foreach (var itm in remoteNode)
        {
            var paths = itm.Path.Split("\\")[2..]; // 路径需要跳过根节点名
            var res = localNode.TryGetValue(out FileSystemInfo? info, paths);
            if (itm.Value != null)
            {
                var tmp = (res && info != null) ? new SynchFileItem(info, itm.Value) : new SynchFileItem(itm.Value);
                result.Insert(new Node<SynchFileItem>(itm.Name, tmp), paths);
            }
        }
        // 遍历本地节点，处理仅在本地存在的节点
        foreach (var itm in localNode)
        {
            var paths = itm.Path.Split("\\")[2..]; // 路径需要跳过根节点名
            var res = remoteNode.TryGetValue(out _, paths);
            if (!res && itm.Value != null)
            {
                var tmp = new SynchFileItem(itm.Value);
                result.Insert(new Node<SynchFileItem>(itm.Name, tmp), paths);
            }
        }
        return result;
    }

    /// <summary>
    /// 更新树形结构中所有列表项的同步状态
    /// </summary>
    /// <param name="root"></param>
    public static void RefreshState(Node<SynchFileItem> root, SynchIgnore? ignore = null)
    {
        ignore ??= new SynchIgnore();
        // 先设置非文件夹节点值和空文件夹节点值的 State
        foreach (var node in root.Where(n => n.Value != null && n.Children.Count == 0))
        {
            SynchFileItem itm = node.Value!;
            if (ignore.Check(node.Path.Replace(root.Name,""))) itm.State = SynchState.Detached;
            else if (itm.IsDir && itm.RemotePath != null && itm.LocalPath != null) itm.State = SynchState.Consistent;
            else if (itm.RemoteUpdate != null && itm.LocalUpdate != null)
            {
                var dt = ((TimeSpan)(itm.RemoteUpdate - itm.LocalUpdate)).TotalSeconds;
                if (dt > 3) itm.State = SynchState.ToUpdate;
                else if (dt < -3) itm.State = SynchState.Modified;
                else itm.State = SynchState.Consistent;
            }
            else if (itm.remoteInfo != null)
                itm.State = SynchState.ToUpdate;
            else
                itm.State = SynchState.Added;
        }
        // 再根据子节点设置文件夹节点值的 State
        foreach (var node in root.Where(n => n.Value != null && n.Children.Count > 0))
        {
            SynchFileItem itm = node.Value!;
            foreach (var child in node.Children)
                if (child.Value?.State != null) itm.State |= child.Value.State;
        }
    }
}
