using CloudDrive.Utils;
using CloudDriveUI.Models;
using CloudDriveUI.Utils;
using ImTools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prism.Commands;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;
using System.Windows;

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

    private PathInfo CurLocalPath => LocalRootPath.Duplicate().Join(CurPath);
    private PathInfo CurRemotePath => RemoteRootPath.Duplicate().Join(CurPath);

    #endregion

    public SynchFileViewModel(ICloudDriveProvider cloudDrive, IOptionsSnapshot<AppConfig> options, ILogger<SynchFileViewModel> logger) : base(cloudDrive, logger)
    {
        AppConfigOpt = options;
        OperationItems = new List<OperationItem>()
        {
            new OperationItem() { Name = "配置同步", Icon = "CogSyncOutline",Command = new DelegateCommand<object?>(obj=>SetAsyncConfig()) },
            new OperationItem() { Name = "刷新列表", Icon = "FolderSyncOutline" ,Command = new DelegateCommand<object?>(async obj=>await RefreshFileItems(true)) },
            new OperationItem() { Name = "立即同步", Icon = "FolderSyncOutline" ,Command = new DelegateCommand<object?>(async obj=>await SynchronizAllItem()) }
        };
        ContextMenuItems = new List<OperationItem>()
        {

        };

        AutoRefleshRoot();
    }

    private async void AutoRefleshRoot()
    {
        while (true)
        {
            await RefreshFileItems(reload: true);
            await Task.Delay(60 * 1000);
        }
    }

    private async Task RefleshRootAsync()
    {
        var remoteNode = await GetRemoteFileNodeAsync((string)RemoteRootPath, true);
        var localNode = FileUtils.GetLocalFileNode((string)LocalRootPath, true);
        root = SyncFileItem.CreateItemNode(localNode, remoteNode);
    }

    /// <summary>
    /// 根据本地文件和远程文件信息设置 FileItems
    /// </summary>
    /// <param name="reload">重载 root</param>
    protected async Task RefreshFileItems(bool reload = false)
    {
        if (root == null || reload) await RefleshRootAsync();
        var res = root?.GetNode((string)CurPath);
        if (res != null)
            FileItems = new ObservableCollection<SyncFileItem>(res.Children.Where(n => n.Value != null).Select(n => n.Value!).OrderByDescending(n => n.IsDir));
    }
    protected override void RefreshFileItems()
    {
        _ = RefreshFileItems(reload: true);
    }

    /// <summary>
    /// 获取云端文件节点
    /// </summary>
    /// <param name="path">根路径</param>
    /// <param name="recursion">是否递归</param>
    /// <returns></returns>
    private async Task<Node<CloudFileInfo>> GetRemoteFileNodeAsync(string path, bool recursion = false)
    {
        IEnumerable<CloudFileInfo> remInfos = new List<CloudFileInfo>();
        if (recursion) remInfos = await cloudDrive.GetFileListAllAsync(path);
        else remInfos = await cloudDrive.GetFileListAsync((PathInfo)path);
        var remote_root = new Node<CloudFileInfo>("remote_root");
        foreach (var itm in remInfos)
        {
            var node = new Node<CloudFileInfo>(itm.Name, itm);
            remote_root.Insert(node, (string)itm.Path);
        }
        remote_root = remote_root.GetNode(path);
        remote_root.Parent = null;
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
                await RefreshFileItems();
            }
            else await DialogHostExtentions.ShowMessageDialog($"{local}不存在！");
        }
    }

    /// <summary>
    /// 全部同步
    /// </summary>
    /// <returns></returns>
    private async Task SynchronizAllItem()
    {
        DialogHostExtentions.ShowCircleProgressBar();
        var remoteNode = await GetRemoteFileNodeAsync((string)RemoteRootPath, true);
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
        await RefreshFileItems(reload: true);
        DialogHostExtentions.CloseCircleProgressBar();
    }


}
