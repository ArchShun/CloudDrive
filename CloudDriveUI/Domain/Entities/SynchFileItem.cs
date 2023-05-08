using CloudDrive.Utils;
using CloudDriveUI.Models;
using CloudDriveUI.Utils;

namespace CloudDriveUI.Domain.Entities;

public class SynchFileItem : FileItemBase
{
    private CloudFileInfo? remoteInfo;
    private FileSystemInfo? localInfo;
    private List<SynchFileItem> children = new();

    #region 构造方法

    /// <summary>
    /// 从远程文件创建文件列表项
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public SynchFileItem(CloudFileInfo info)
    {
        remoteInfo = info;
    }

    /// <summary>
    /// 从文件系统创建列表项
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public SynchFileItem(FileSystemInfo info)
    {
        localInfo = info;
    }

    /// <summary>
    /// 根据本地文件信息和远程文件信息创建文件列表项
    /// </summary>
    /// <param name="local"></param>
    /// <param name="remote"></param>
    /// <returns></returns>
    public SynchFileItem(FileSystemInfo local, CloudFileInfo remote)
    {
        remoteInfo = remote;
        localInfo = local;
    }

    #endregion

    #region 属性
    public override string Id { get; } = Guid.NewGuid().ToString();
    public override string Name => localInfo != null ? localInfo.Name : remoteInfo?.Name ?? "";
    public override bool IsDir => remoteInfo?.IsDir ?? (localInfo!.Attributes & FileAttributes.Directory) > 0;
    public override FileType FileType => IsDir ? FileType.Unknown : remoteInfo?.Category ?? FileUtils.GetFileType((FileInfo)localInfo!);
    public override string Size => IsDir ? "--" : FileUtils.CalSize(remoteInfo?.Size ?? ((FileInfo)localInfo!).Length);
    public DateTime? RemoteUpdate
    {
        get
        {
            if (remoteInfo != null)
                return DateTimeUtils.TimeSpanToDateTime(remoteInfo.LocalMtime ?? remoteInfo.ServerMtime);
            else return null;
        }
    }

    public DateTime? LocalUpdate => localInfo?.LastWriteTime;

    /// <summary>
    /// 同步状态，根据最后修改时间确定, 文件夹节点只标注修改
    /// </summary>
    public SynchState State
    {
        get
        {
            if (remoteInfo == null && localInfo == null)
                throw new ArgumentNullException("内部逻辑错误，本地路径和远程路径同时为空");
            else if (remoteInfo != null && localInfo == null)
                return SynchState.RemoteAdded;
            else if (remoteInfo == null && localInfo != null)
                return SynchState.Added;
            else if (RemoteUpdate == null || LocalUpdate == null)
                throw new ArgumentNullException("内部逻辑错误，本地更新时间和远程更新时间此时不应该为空");
            // 本地与远程均存在的文件夹，根据子文件状态判定
            else if (IsDir)
            {
                if (Children.Count == 0)
                    return Math.Abs(((TimeSpan)(RemoteUpdate - LocalUpdate)).TotalSeconds) < 3 ? SynchState.Consistent : SynchState.Unknown;
                var sts = Children[0].State;
                foreach (var tmp in Children)
                    sts |= tmp.State;
                return sts;
            }
            else
            {
                var dt = ((TimeSpan)(RemoteUpdate - LocalUpdate)).TotalSeconds;
                if (dt > 3) return SynchState.RemoteModified;
                else if (dt < -3) return SynchState.Modified;
                else return SynchState.Consistent;
            }
        }
    }
    public PathInfo? LocalPath => localInfo != null ? (PathInfo)localInfo.FullName : null;
    public PathInfo? RemotePath => remoteInfo?.Path;

    /// <summary>
    /// 子节点
    /// </summary>
    public List<SynchFileItem> Children
    {
        get => children; set
        {
            children = value;
            RaisePropertyChanged();
        }
    }
    /// <summary>
    /// 父节点
    /// </summary>
    public SynchFileItem? Parent { get; set; } = null;
    /// <summary>
    /// 兄弟节点
    /// </summary>
    public List<SynchFileItem> Sibling => Parent == null ? new List<SynchFileItem>() : Parent.Children.Where(n => n.Name != Name).ToList();
    /// <summary>
    /// 根节点
    /// </summary>
    public SynchFileItem Root => Parent == null ? this : Parent.Root;

    #endregion

    private void RaiseStateChange()
    {
        RaisePropertyChanged(nameof(State));
        if (Parent != null)
            Parent.RaiseStateChange();
    }

    public void AddChild(SynchFileItem child)
    {
        child.Parent = this;
        Children.Add(child);
        RaisePropertyChanged(nameof(Children));
        RaiseStateChange();
    }
    public void AddChild(SynchFileItem child, string[] path)
    {
        if (path.Length == 0) AddChild(child);
        else
        {
            SynchFileItem? parent = Children.Find(e => e.Name == path[0]) ?? throw new ArgumentException($"{string.Join('/', path)}路径不存在");
            parent.AddChild(child, path[1..]);
        }
    }

    public void AddChildren(IEnumerable<SynchFileItem> children)
    {
        foreach (var child in children) AddChild(child);
    }

    public SynchFileItem GetChild(string[] path)
    {
        if (path.Length == 0) return this;
        SynchFileItem? parent = Children.Find(e => e.Name == path[0]) ?? throw new ArgumentException($"{string.Join('/', path)}路径不存在");
        return parent.GetChild(path[1..]);
    }

    public void ChangeRemoteInfo(CloudFileInfo cloudFileInfo)
    {
        remoteInfo = cloudFileInfo;
        RaisePropertyChanged(nameof(remoteInfo));
    }

    public void ChangeLocalInfo(FileSystemInfo fileSystemInfo)
    {
        localInfo = fileSystemInfo;
        RaisePropertyChanged(nameof(localInfo));
    }

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
                var tmp = res && info != null ? new SynchFileItem(info, itm.Value) : new SynchFileItem(itm.Value);
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
        //ignore ??= new SynchIgnore();
        //// 先根据最后修改时间设置文件和文件夹 State
        //foreach (var node in root.Where(n => n.Value != null))
        //{
        //    SynchFileItem itm = node.Value!;
        //    if (ignore.Check(node.Path.Replace(root.Name, ""))) itm.State = SynchState.Detached;
        //    else if (itm.RemoteUpdate != null && itm.LocalUpdate != null)
        //    {
        //        var dt = ((TimeSpan)(itm.RemoteUpdate - itm.LocalUpdate)).TotalSeconds;
        //        if (dt > 3) itm.State = SynchState.ToUpdate;
        //        else if (dt < -3) itm.State = SynchState.Modified;
        //        else itm.State = SynchState.Consistent;
        //    }
        //    else if (itm.remoteInfo != null)
        //        itm.State = SynchState.ToUpdate;
        //    else
        //        itm.State = SynchState.Added;
        //}
        //// 再根据子节点设置文件夹节点值的 State
        //foreach (var node in root.Where(n => n.Value != null && n.Children.Count > 0))
        //{
        //    SynchFileItem itm = node.Value!;
        //    foreach (var child in node.Children)
        //        if (child.Value?.State != null) itm.State |= child.Value.State;
        //}
    }
}
