using CloudDrive.Utils;
using CloudDriveUI.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prism.Commands;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CloudDriveUI.ViewModels;

public class SynchFileViewModel : FileViewBase
{
    protected ObservableCollection<SyncFileItem> fileItems = new();
    private Node<SyncFileItem>? root;

    #region 属性

    /// <summary>
    /// 需要显示的文件列表
    /// </summary>
    public ObservableCollection<SyncFileItem> FileItems { get => fileItems; set => SetProperty(ref fileItems, value); }

    public IOptionsSnapshot<AppConfig> AppConfigOpt { get; private set; }

    private PathInfo LocalRootPath => new PathInfo(Path.GetFullPath(AppConfigOpt.Value.SynchFileConfig.LocalPath)).Lock();
    private PathInfo RemoteRootPath => new PathInfo(AppConfigOpt.Value.SynchFileConfig.RemotePath).Lock();
    private PathInfo RemoteBackupPath => RemoteRootPath.Join(".backup").Lock();
    private PathInfo CurLocalPath => LocalRootPath.Duplicate().Join(CurPath);
    private PathInfo CurRemotePath => RemoteRootPath.Duplicate().Join(CurPath);

    #endregion

    #region 命令
    public DelegateCommand<SyncFileItem> SynchronizItemCommand { get; set; }
    public DelegateCommand<SyncFileItem> RenameCommand { get; set; }
    public DelegateCommand<SyncFileItem> DeleteCommand { get; set; }
    public DelegateCommand<SyncFileItem> IgnoreCommand { get; set; }
    public DelegateCommand CreateDirCommand { get; set; }
    public DelegateCommand SynchronizAllCommand { get; set; }
    public DelegateCommand RefreshCommand { get; set; }

    #endregion

    public SynchFileViewModel(ICloudDriveProvider cloudDrive, IOptionsSnapshot<AppConfig> options, ILogger<SynchFileViewModel> logger, ISnackbarMessageQueue snackbarMessageQueue) : base(cloudDrive, logger, snackbarMessageQueue)
    {
        AppConfigOpt = options;
        OperationItems = new List<OperationItem>()
        {
            new OperationItem() { Name = "配置同步", Icon = "CogSyncOutline",Command = new DelegateCommand<object?>(obj=>SetAsyncConfig()) },
            new OperationItem() { Name = "刷新列表", Icon = "FolderSyncOutline" ,Command = new DelegateCommand<object?>( obj=> RefreshFileItems(true)) },
            new OperationItem() { Name = "立即同步", Icon = "FolderSyncOutline" ,Command = new DelegateCommand<object?>(async obj=>await SynchronizAll()) }
        };
        SynchronizItemCommand = new(itm => SynchronizItemAsync(itm));
        RenameCommand = new(itm => RenameItem(itm));
        DeleteCommand = new(itm => DeleteItem(itm));
        IgnoreCommand = new(itm => IgnoreItem(itm)); ;
        CreateDirCommand = new(() => CreateDir()); ;
        SynchronizAllCommand = new(async () => await SynchronizAll());
        RefreshCommand = new(() => RefreshFileItems());

        if (AppConfigOpt.Value.SynchFileConfig.AutoRefresh) AutoRefleshRoot();
        else RefreshFileItems(reload: true);
    }

    private object CreateDir()
    {
        throw new NotImplementedException();
    }

    private object IgnoreItem(SyncFileItem itm)
    {
        throw new NotImplementedException();
    }

    private object DeleteItem(SyncFileItem itm)
    {
        throw new NotImplementedException();
    }

    private object RenameItem(SyncFileItem itm)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// 同步节点及其子节点上的所有数据
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private async Task SynchronizNodeAsync(Node<SyncFileItem> node)
    {
        Stack<Node<SyncFileItem>> stack = new Stack<Node<SyncFileItem>>();
        stack.Push(node);
        while (stack.Count > 0)
        {
            Node<SyncFileItem> _node = stack.Pop();
            if (_node.Value is not SyncFileItem itm) continue;
            bool flag = false;
            var sta = itm.State;
            // 新增，则上传文件/文件夹
            if (sta == SynchState.Added)
            {
                PathInfo relative = itm.LocalPath!.GetRelative(LocalRootPath);
                PathInfo remotePath = RemoteRootPath.Join(relative);
                flag = itm.IsDir
                ? (await cloudDrive.UploadDirAsync(itm.LocalPath, remotePath)).Any()
                : (await cloudDrive.UploadAsync(itm.LocalPath!, remotePath)) == null;
            }
            // 删除，软删除文件/整个文件夹（云端备份），同时删除子节点
            else if (sta == SynchState.Deleted)
            {
                PathInfo remoteBackupPath = RemoteBackupPath.Join(itm.RemotePath!.GetRelative(RemoteRootPath).GetFullPath() + DateTime.Now.ToLongDateString());
                string localPath = LocalRootPath.Join(itm.LocalPath!.GetRelative(LocalRootPath)).GetFullPath();
                try
                {
                    flag = await cloudDrive.MoveAsync(itm.RemotePath, remoteBackupPath); //移动备份
                    if (flag)
                    {
                        if (itm.IsDir) Directory.Delete(localPath);
                        else File.Delete(localPath);
                    }
                }
                catch
                {
                    flag = false;
                }
                _node.Children.Clear();
            }
            // 修改，备份文件后再上传
            else if (sta == SynchState.Modified)
            {
                PathInfo remoteBackupPath = RemoteBackupPath.Join(itm.RemotePath!.GetRelative(RemoteRootPath).GetFullPath() + DateTime.Now.ToLongDateString());
                flag &= await cloudDrive.MoveAsync(itm.RemotePath, remoteBackupPath);
                flag &= itm.IsDir
                    ? (await cloudDrive.UploadDirAsync(itm.LocalPath!, itm.RemotePath)).Any()
                    : (await cloudDrive.UploadAsync(itm.LocalPath!, itm.RemotePath)) == null;
            }
            // 下载网盘文件
            else if (itm.State == SynchState.ToUpdate)
            {
                PathInfo localPath = LocalRootPath.Join(itm.RemotePath!.GetRelative(RemoteRootPath));
                // 如果不是文件夹，下载文件并设置修改时间为网盘时间
                if (!itm.IsDir)
                {
                    FileInfo fileInfo = new(localPath.GetFullPath());
                    fileInfo.Directory?.Create();
                    flag = await cloudDrive.DownloadAsync(itm.RemotePath!, (PathInfo)fileInfo.FullName);
                    if (flag) fileInfo.LastWriteTime = itm.RemoteUpdate ?? DateTime.Now;
                }
                // 如果是空文件夹，直接创建文件夹,，修改创建时间为云盘时间
                else if (!node.Children.Any())
                {
                    DirectoryInfo info = Directory.CreateDirectory(localPath.GetFullPath());
                    info.LastAccessTime = (DateTime)itm.RemoteUpdate!;
                    flag = true;
                }
            }
            // 文件夹节点的子节点包含多种修改则先处理子节点，最后处理父节点
            else if (sta.HasFlag(SynchState.Added) || sta.HasFlag(SynchState.Deleted) || sta.HasFlag(SynchState.Modified))
            {
                stack.Push(_node);//当前节点入栈，待子节点处理完成最后处理
                foreach (var child in _node.Children) stack.Push(child); // 子节点依次入栈等待处理
                flag = true;
            }
            if (!flag) snackbar.Enqueue($"{_node.Path}同步失败", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }
    }
    private async Task SynchronizItemAsync(SyncFileItem itm)
    {
        if (itm == null) return;
        if (!(root?.TryGetNode(CurPath.Join(itm.Name).GetFullPath(), out Node<SyncFileItem>? node) ?? false)) return;
        await SynchronizNodeAsync(node!);
    }

    /// <summary>
    /// 全部同步
    /// </summary>
    /// <returns></returns>
    private async Task SynchronizAll()
    {
        DialogHostExtentions.ShowCircleProgressBar();
        var remoteNode = await GetRemoteFileNodeAsync(RemoteRootPath, true);
        var localNode = FileUtils.GetLocalFileNode((string)LocalRootPath, true);
        var itmeNode = SyncFileItem.CreateItemNode(localNode, remoteNode);
        // 同步操作       
        foreach (var node in itmeNode)
        {
            if (node.Value == null) continue;
            switch (node.Value.State)
            {
                case SynchState.Added:
                    {
                        PathInfo relative = node.Value.LocalPath!.GetRelative(LocalRootPath);
                        PathInfo remotePath = RemoteRootPath.Duplicate().Join(relative);
                        var res = node.Value.IsDir ? await cloudDrive.CreateDirectoryAsync(remotePath) : await cloudDrive.UploadAsync(node.Value.LocalPath!, remotePath);
                        if (res == null) logger.LogError("{RemotePath}创建失败", remotePath);
                    }
                    break;
                case SynchState.Modified:
                    {
                        var res = node.Value.IsDir ? null : await cloudDrive.UploadAsync(node.Value.LocalPath!, node.Value.RemotePath!);
                        if (res == null) logger.LogError("{LocalPath}-->{RemotePath}修改失败", node.Value.LocalPath, node.Value.RemotePath);
                    }
                    break;
                case SynchState.ToUpdate:
                    {
                        PathInfo localPath = LocalRootPath.Duplicate().Join(node.Value.RemotePath!.GetRelative(RemoteRootPath));
                        // 如果不是文件夹，下载文件并设置修改时间为网盘时间
                        if (!node.Value.IsDir)
                        {
                            FileInfo fileInfo = new(localPath.GetFullPath());
                            fileInfo.Directory?.Create();
                            if (await cloudDrive.DownloadAsync(node.Value.RemotePath!, (PathInfo)fileInfo.FullName))
                                fileInfo.LastWriteTime = node.Value.RemoteUpdate ?? DateTime.Now;
                            else logger.LogError("{RemotePath}-->{LocalPath}修改失败", node.Value.RemotePath, fileInfo.FullName);
                        }
                        // 如果是空文件夹，直接创建文件夹
                        else if (node.Children.Count == 0) Directory.CreateDirectory(localPath.GetFullPath());
                    }
                    break;
            }
        }
        // 更新
        RefreshFileItems(reload: true);
        DialogHostExtentions.CloseCircleProgressBar();
    }




    private async void AutoRefleshRoot()
    {
        while (true)
        {
            RefreshFileItems(reload: true);
            await Task.Delay(AppConfigOpt.Value.SynchFileConfig.AutoRefreshSeconds * 1000);
        }
    }


    /// <summary>
    /// 根据本地文件和远程文件信息设置 FileItems
    /// </summary>
    /// <param name="reload">重载 root</param>
    protected async void RefreshFileItems(bool reload = false)
    {
        if (root == null || reload)
        {
            var remoteNode = await GetRemoteFileNodeAsync(RemoteRootPath, true);
            var localNode = FileUtils.GetLocalFileNode((string)LocalRootPath, true);
            root = SyncFileItem.CreateItemNode(localNode, remoteNode);
        }
        var res = root?.GetNode((string)CurPath);
        if (res != null)
            FileItems = new ObservableCollection<SyncFileItem>(res.Children.Where(n => n.Value != null).Select(n => n.Value!).OrderByDescending(n => n.IsDir));
    }
    protected override void RefreshFileItems()
    {
        RefreshFileItems(reload: true);
    }

    /// <summary>
    /// 获取云端文件节点
    /// </summary>
    /// <param name="path">根路径</param>
    /// <param name="recursion">是否递归</param>
    /// <returns></returns>
    private async Task<Node<CloudFileInfo>> GetRemoteFileNodeAsync(PathInfo path, bool recursion = false)
    {
        IEnumerable<CloudFileInfo> remInfos = new List<CloudFileInfo>();
        if (recursion) remInfos = await cloudDrive.GetFileListAllAsync(path);
        else remInfos = await cloudDrive.GetFileListAsync(path);
        var remote_root = new Node<CloudFileInfo>("remote_root");
        foreach (var itm in remInfos)
        {
            var node = new Node<CloudFileInfo>(itm.Name, itm);
            remote_root.Insert(node, (string)itm.Path);
        }
        if (remote_root.TryGetNode((string)path, out Node<CloudFileInfo>? pathNode))
        {
            pathNode!.Parent = null;
            return pathNode;
        }
        return remote_root;
    }

    /// <summary>
    /// 配置同步路径
    /// </summary>
    private async void SetAsyncConfig()
    {
        var conf = AppConfigOpt.Value.SynchFileConfig ?? new SynchFileConfig();
        Dictionary<string, string?> pair = new() { { "LocalPath", (string)LocalRootPath }, { "RemotePath", (string)RemoteRootPath } };
        var res = await DialogHostExtentions.ShowListDialogAsync(pair);
        if (res)
        {
            pair.TryGetValue("LocalPath", out string? local);
            pair.TryGetValue("RemotePath", out string? remote);
            if (Directory.Exists(local) && remote != null)
            {
                conf.LocalPath = local;
                conf.RemotePath = remote;
                RefreshFileItems();
            }
            else await DialogHostExtentions.ShowMessageDialog($"{local}不存在！");
        }
    }


}
