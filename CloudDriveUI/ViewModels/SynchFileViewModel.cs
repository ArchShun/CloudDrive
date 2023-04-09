﻿using CloudDrive.Utils;
using CloudDriveUI.Models;
using CloudDriveUI.Utils;
using ImTools;
using Microsoft.Extensions.Options;
using Prism.Commands;
using System;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;
using System.Windows;

namespace CloudDriveUI.ViewModels;

public class SynchFileViewModel : FileViewBase
{
    protected ObservableCollection<SyncFileItem> fileItems = new();

    #region 属性

    /// <summary>
    /// 需要显示的文件列表
    /// </summary>
    public ObservableCollection<SyncFileItem> FileItems { get => fileItems; set => SetProperty(ref fileItems, value); }

    public IEnumerable<OperationItem> OperationItems { get; }

    public IOptionsSnapshot<AppConfig> AppConfigOpt { get; private set; }

    private string LocalRootPath => Path.GetFullPath(AppConfigOpt.Value.SynchFileConfig.LocalPath.Replace("/", "\\"));
    private string RemoteRootPath => AppConfigOpt.Value.SynchFileConfig.RemotePath.Replace("/", "\\");

    private string CurLocalPath => Path.Join(LocalRootPath, string.Join("\\", Paths.Skip(1)));
    private string CurRemotePath => Path.Join(RemoteRootPath, string.Join("\\", Paths.Skip(1)));

    #endregion

    public SynchFileViewModel(ICloudDriveProvider cloudDrive, IOptionsSnapshot<AppConfig> options) : base(cloudDrive)
    {
        AppConfigOpt = options;
        OperationItems = new ObservableCollection<OperationItem>()
        {
            new OperationItem() { Name = "配置同步", Icon = "CogSyncOutline",Command = new DelegateCommand<object?>(obj=>SetAsyncConfig()) },
            new OperationItem() { Name = "刷新列表", Icon = "FolderSyncOutline" ,Command = new DelegateCommand<object?>(async obj=>await RefreshFileItems(CurLocalPath,CurRemotePath)) },
            new OperationItem() { Name = "立即同步", Icon = "FolderSyncOutline" ,Command = new DelegateCommand<object?>(async obj=>await SynchronizAllItem()) }
        };

        _ = RefreshFileItems(CurLocalPath, CurRemotePath);
    }
    protected override async void OpenDirAsync(FileItemBase itm)
    {
        if (itm.IsDir)
        {
            try
            {
                Paths.Add(itm.Name ?? "");
                await RefreshFileItems(CurLocalPath, CurRemotePath);
            }
            catch (Exception ex)
            {
                Paths.RemoveAt(Paths.Count - 1);
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    protected override void NavDir(int? i)
    {
        if (i.HasValue)
        {
            var end = i.Value + 1;
            Paths = new ObservableCollection<string>(Paths.ToArray()[..end]);
            _ = RefreshFileItems(CurLocalPath, CurRemotePath);
        }
    }

    /// <summary>
    /// 根据本地文件和远程文件信息设置 FileItems
    /// </summary>
    /// <param name="localInfos">是否更新本地文件信息</param>
    /// <param name="remoteInfos">是否更新远程文件信息</param>
    /// <returns>FileListItem 集合对象 </returns>
    protected async Task RefreshFileItems(string localPath, string remotePath)
    {
        var remoteNode = await GetRemoteFileNodeAsync(remotePath, false, true);
        var localNode = FileUtils.GetLocalFileNode(localPath, false, true);

        var res = SyncFileItem.CreateItems(localNode, remoteNode);
        FileItems = new ObservableCollection<SyncFileItem>(res);
    }


    /// <summary>
    /// 获取云端文件节点
    /// </summary>
    /// <param name="path">根路径</param>
    /// <param name="recursion">是否递归</param>
    /// <param name="relocation">是否将根路径设置到 path </param>
    /// <returns></returns>
    private async Task<Node<CloudFileInfo>> GetRemoteFileNodeAsync(string path, bool recursion = false, bool relocation = true)
    {
        IEnumerable<CloudFileInfo> remInfos = new List<CloudFileInfo>();
        if (recursion) remInfos = await cloudDrive.GetFileListAllAsync(path);
        else remInfos = await cloudDrive.GetFileListAsync(path);
        var remote_root = new Node<CloudFileInfo>("remote_root");
        foreach (var itm in remInfos)
        {
            var node = new Node<CloudFileInfo>(itm.Name, itm);
            remote_root.Insert(node, itm.Path);
        }
        if (relocation)
        {
            remote_root = remote_root.GetNode(path);
            remote_root.Parent = null;
        }
        return remote_root;
    }

    /// <summary>
    /// 配置同步路径
    /// </summary>
    private async void SetAsyncConfig()
    {
        var conf = AppConfigOpt.Value.SynchFileConfig ?? new SynchFileConfig();
        var pair = new Dictionary<string, string?>() { { "LocalPath", LocalRootPath }, { "RemotePath", RemoteRootPath } };
        var res = await DialogHostExtentions.ShowListDialogAsync(pair);
        if (res)
        {
            pair.TryGetValue("LocalPath", out string? local);
            pair.TryGetValue("RemotePath", out string? remote);
            if (Directory.Exists(local) && remote != null)
            {
                conf.LocalPath = local;
                conf.RemotePath = remote;
                await RefreshFileItems(CurLocalPath, CurRemotePath);
            }
            else await DialogHostExtentions.ShowMessageDialogAsync($"{local}不存在！");
        }
    }

    /// <summary>
    /// 全部同步
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    private async Task SynchronizAllItem()
    {
        DialogHostExtentions.ShowCircleProgressBar();
        var remoteNode = await GetRemoteFileNodeAsync(RemoteRootPath, true, true);
        var localNode = FileUtils.GetLocalFileNode(LocalRootPath, true, true);
        var fileItems = SyncFileItem.CreateItems(localNode, remoteNode);
        // 同步操作
        foreach (var itm in fileItems)
        {
            if (!itm.IsDir) switch (itm.State)
                {
                    case SynchState.Added:
                        var remotePath = Path.Join(RemoteRootPath, Path.GetRelativePath(LocalRootPath, itm.LocalPath!));
                        await cloudDrive.UploadAsync(itm.LocalPath!, remotePath);
                        break;
                    case SynchState.Modified:
                        await cloudDrive.UploadAsync(itm.LocalPath!, itm.RemotePath!);
                        break;
                    case SynchState.ToUpdate:
                        var localFile = new FileInfo(Path.Join(LocalRootPath, Path.GetRelativePath(RemoteRootPath, itm.RemotePath!)));
                        if (localFile != null)
                        {
                            if ((!localFile.Directory?.Exists) ?? false) localFile.Directory?.Create();
                            var res = await cloudDrive.DownloadAsync(itm.RemotePath!, localFile.FullName);
                            if (res)
                                localFile.LastWriteTime = (DateTime)itm.RemoteUpdate!;
                        }
                        break;
                }
        }
        // 更新
        await RefreshFileItems(CurLocalPath, CurRemotePath);
        DialogHostExtentions.CloseCircleProgressBar();
    }

}
