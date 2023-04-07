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

    #region 属性
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

    private string CurLocalPath
    {
        get
        {
            var conf = AppConfigOpt.Value.SynchFileConfig;
            return Path.TrimEndingDirectorySeparator(Path.Join(conf.LocalPath.Replace("/", "\\"), string.Join("\\", Paths.Skip(1))));
        }
    }
    private string CurRemotePath
    {
        get
        {
            var conf = AppConfigOpt.Value.SynchFileConfig;
            return Path.TrimEndingDirectorySeparator(Path.Join(conf.RemotePath.Replace("/", "\\"), string.Join("\\", Paths.Skip(1))));
        }
    }

    #endregion

    public SynchFileViewModel(ICloudDriveProvider cloudDrive, IOptionsSnapshot<AppConfig> options) : base(cloudDrive)
    {
        AppConfigOpt = options;
        OperationItems = new ObservableCollection<OperationItem>()
        {
            new OperationItem() { Name = "配置同步", Icon = "CogSyncOutline",Command = new DelegateCommand<object?>(obj=>SetAsyncConfig()) },
            new OperationItem() { Name = "立即同步", Icon = "FolderSyncOutline" ,Command = new DelegateCommand<object?>(async obj=>await SynchronizAllItem()) }
        };

        _ = RefreshFileItems(CurLocalPath, CurRemotePath);
    }

    protected override async void OpenDir(string id)
    {
        var itm = fileItems.FirstOrDefault(e => e?.Id == id && e.IsDir, null);
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
    protected async Task RefreshFileItems(string loc_path, string rem_path)
    {
        var result = new ObservableCollection<FileListItem>();
        try
        {
            var loc = new Dictionary<string, FileSystemInfo>();
            var rem = new Dictionary<string, CloudFileInfo>();
            if (loc_path != null && Directory.Exists(loc_path))
            {
                var res = new DirectoryInfo(loc_path).GetFileSystemInfos();
                foreach (var info in res)
                {
                    var isDir = (info!.Attributes & FileAttributes.Directory) > 0;
                    var k = $"{isDir}-{Path.GetRelativePath(loc_path, info.FullName)}";
                    loc.Add(k, info);
                }
            }
            try
            {
                if (rem_path != null)
                {
                    var res = await cloudDrive.GetFileListAsync(rem_path);
                    foreach (var info in res)
                    {
                        var k = $"{info.IsDir}-{Path.GetRelativePath(rem_path, info.Path)}";
                        rem.Add(k, info);
                    }
                }
            }
            catch { }
            // 遍历本地文件，如果远程也存在则根据本地和远程创建列表项
            foreach (var kv in loc)
            {
                if (rem.ContainsKey(kv.Key)) result.Add(new FileListItem(kv.Value, rem[kv.Key]));
                else result.Add(new FileListItem(kv.Value));
            }
            // 遍历远程文件，如果本地不存在，则创建列表项
            foreach (var kv in rem)
            {
                if (!loc.ContainsKey(kv.Key)) result.Add(new FileListItem(kv.Value));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        this.FileItems = result;
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

    private async Task<bool> UploadItem(FileListItem itm)
    {
        var path = Path.Join(CurLocalPath, itm.Name);
        var dest = Path.Join(CurRemotePath, itm.Name);
        if (!itm.IsDir)
        {
            var res = await cloudDrive.UploadAsync(path, dest);
            if (res != null)
            {
                itm.State = SynchState.Consistent;
                RaisePropertyChanged(nameof(FileItems));
                return true;
            }
        }
        return false;
    }

    private async Task<bool> DeletedItem(int idx)
    {
        var itm = this.FileItems[idx];
        var dest = CurLocalPath + itm.Name;
        var res = await cloudDrive.DeleteAsync(dest);
        if (res)
        {
            FileItems.RemoveAt(idx);
            RaisePropertyChanged(nameof(FileItems));
            return true;
        }
        return false;
    }

    private async Task<bool> DownloadItem(FileListItem itm)
    {
        var path = CurLocalPath + itm.Name;
        var dest = CurLocalPath + itm.Name;
        var res = await cloudDrive.DownloadAsync(path, dest);
        if (res)
        {
            itm.State = SynchState.Consistent;
            RaisePropertyChanged(nameof(FileItems));
            return true;
        }
        return false;
    }

    private async Task<bool> UploadDir(FileListItem itm)
    {
        var path = CurLocalPath + itm.Name;
        var dest = CurLocalPath + itm.Name;
        var remoteInfo = await cloudDrive.GetFileListAllAsync(dest);
        var localInfo = new List<FileListItem>();
        //FileUtils.GetLocalFileInfos(path, ref localInfo, FileListItemUtils.Create, recursion: true);

        if (await cloudDrive.CreateDirectoryAsync(dest) != null)
        {
            //var mapper = FileListItemUtils.MergeItems(localInfo, remoteInfo);
            //FileListItemUtils.CreateFileItemState(LocalFileInfos, RemoteFileInfos, Mapper);
        }
        return false;
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
            var paths = itm.Path.Split("/").Where(e => !string.IsNullOrEmpty(e)).ToArray();
            remote_root.Insert(node, paths);
        }
        if (relocation)
        {
            remote_root = remote_root.GetNode(path, '/');
            remote_root.Parent = null;
        }
        return remote_root;
    }


    /// <summary>
    /// 全部同步
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    private async Task SynchronizAllItem()
    {
        var remote_root = await GetRemoteFileNodeAsync(AppConfigOpt.Value.SynchFileConfig.RemotePath, true, true);
        var local_root = FileUtils.GetLocalFileNode(AppConfigOpt.Value.SynchFileConfig.LocalPath, true, true);

        var result = new ObservableCollection<FileListItem>();
        foreach (var itm in remote_root)
        {
            var info = local_root.FirstOrDefault(node => {
                Console.WriteLine(node?.Path +"----->"+ itm.Path);
               return node?.Path == itm.Path;
            }, null);
            if (info?.Value != null && itm.Value != null) result.Add(new FileListItem(info.Value, itm.Value));
            else if (itm.Value != null) result.Add(new FileListItem(itm.Value));
        }
        //try
        //{
        //    var loc = new Dictionary<string, FileSystemInfo>();
        //    var rem = new Dictionary<string, CloudFileInfo>();
        //    if (loc_path != null && Directory.Exists(loc_path))
        //    {
        //        var res = new DirectoryInfo(loc_path).GetFileSystemInfos();
        //        foreach (var info in res)
        //        {
        //            var isDir = (info!.Attributes & FileAttributes.Directory) > 0;
        //            var k = $"{isDir}-{Path.GetRelativePath(loc_path, info.FullName)}";
        //            loc.Add(k, info);
        //        }
        //    }
        //    try
        //    {
        //        if (rem_path != null)
        //        {
        //            var res = await cloudDrive.GetFileListAsync(rem_path);
        //            foreach (var info in res)
        //            {
        //                var k = $"{info.IsDir}-{Path.GetRelativePath(rem_path, info.Path)}";
        //                rem.Add(k, info);
        //            }
        //        }
        //    }
        //    catch { }
        //    // 遍历本地文件，如果远程也存在则根据本地和远程创建列表项
        //    foreach (var kv in loc)
        //    {
        //        if (rem.ContainsKey(kv.Key)) result.Add(new FileListItem(kv.Value, rem[kv.Key]));
        //        else result.Add(new FileListItem(kv.Value));
        //    }
        //    // 遍历远程文件，如果本地不存在，则创建列表项
        //    foreach (var kv in rem)
        //    {
        //        if (!loc.ContainsKey(kv.Key)) result.Add(new FileListItem(kv.Value));
        //    }
        //}
        //catch (Exception ex)
        //{
        //    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //}

        //this.FileItems = result;


        //foreach (var itm in FileItems)
        //{
        //    //if (itm.IsDir) switch (itm.State)
        //    //    {
        //    //        case SynchState.Added:
        //    //            return UploadDir(itm);
        //    //        case SynchState.Modified:
        //    //            return UploadItem(itm);
        //    //        case SynchState.Deleted:
        //    //            return DeletedItem(idx);
        //    //        case SynchState.ToUpdate:
        //    //            return DownloadItem(itm);
        //    //    }
        //    if (!itm.IsDir) switch (itm.State)
        //        {
        //            case SynchState.Added:
        //            case SynchState.Modified:
        //                await UploadItem(itm);
        //                break;
        //                //case SynchState.Deleted:
        //                //    return DeletedItem(idx);
        //                //case SynchState.ToUpdate:
        //                //    return DownloadItem(itm);
        //        }

        //}
    }


    public bool Push()
    {
        throw new NotImplementedException();
    }

    #endregion

}
