using CloudDrive.Utils;
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
    protected ObservableCollection<FileListItem> fileItems = new();

    #region 属性

    /// <summary>
    /// 需要显示的文件列表
    /// </summary>
    public ObservableCollection<FileListItem> FileItems { get => fileItems; set => SetProperty(ref fileItems, value); }

    /// <summary>
    /// 本地与远程文件id映射
    /// </summary>
    public IEnumerable<Tuple<string?, long?>> Mapper { get; set; } = new List<Tuple<string?, long?>>();

    /// <summary>
    /// 云端对应文件列表
    /// </summary>
    public IEnumerable<CloudFileInfo> RemoteFileInfos { get; set; } = new List<CloudFileInfo>();

    /// <summary>
    /// 当前目录本地文件
    /// </summary>
    public IEnumerable<FileSystemInfo> LocalFileInfos { get; set; } = new List<FileSystemInfo>();

    public IEnumerable<OperationItem> OperationItems { get; }

    public IOptionsSnapshot<AppConfig> AppConfigOpt { get; private set; }

    private string LocalRootPath
    {
        get => Path.GetFullPath(AppConfigOpt.Value.SynchFileConfig.LocalPath.Replace("/", "\\"));
    }
    private string RemoteRootPath { get => AppConfigOpt.Value.SynchFileConfig.RemotePath.Replace("/", "\\"); }

    private string CurLocalPath
    {
        get => Path.Join(LocalRootPath, string.Join("\\", Paths.Skip(1)));
    }
    private string CurRemotePath
    {
        get => Path.Join(RemoteRootPath, string.Join("\\", Paths.Skip(1)));
    }

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

    protected override async void OpenDir(object id)
    {
        var itm = fileItems.FirstOrDefault(e => e?.Id == id.ToString() && e.IsDir, null);
        if (itm != null)
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

        var res = FileListItem.CreateFileItems(localNode, remoteNode);
        FileItems = new ObservableCollection<FileListItem>(res);
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


    #region 配置相关
    /// <summary>
    /// 配置同步路径
    /// </summary>
    private async void SetAsyncConfig()
    {
        var conf = AppConfigOpt.Value.SynchFileConfig ?? new SynchFileConfig();
        var pair = new Dictionary<string, string?>() { { "LocalPath", conf.LocalPath }, { "RemotePath", conf.RemotePath } };
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
            else
            {
                MessageBox.Show($"{local}不存在！", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region 文件同步操作

 
    private async Task<bool> DeletedItem(FileListItem itm)
    {
        var dest = Path.Join(CurLocalPath, itm.Name);
        var res = await cloudDrive.DeleteAsync(dest);
        return res;
    }

    private async Task<bool> DownloadItem(FileListItem itm)
    {
        var path = Path.Join(CurLocalPath, itm.Name);
        var dest = Path.Join(CurLocalPath, itm.Name);
        var res = await cloudDrive.DownloadAsync(path, dest);
        return res;
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
        var fileItems = FileListItem.CreateFileItems(localNode, remoteNode);
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
                        //case SynchState.Deleted:
                        //    await DeletedItem(itm);
                        //    break;
                        //case SynchState.ToUpdate:
                        //    await DownloadItem(itm);
                        //    break;
                }
        }
        // 更新
        await RefreshFileItems(CurLocalPath, CurRemotePath);

        DialogHostExtentions.CloseCircleProgressBar();
    }


    #endregion

}
