using CloudDrive.Utils;
using CloudDriveUI.Configurations;
using CloudDriveUI.Models;
using CloudDriveUI.PubSubEvents;
using ImTools;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CloudDriveUI.ViewModels;

public class SynchFileViewModel : FileViewBase
{
    protected ObservableCollection<SynchFileItem> fileItems = new();
    private Node<SynchFileItem>? root;
    private readonly IRegionManager regionManager;
    private CancellationTokenSource autoSynchCts = new();
    #region 属性

    /// <summary>
    /// 需要显示的文件列表
    /// </summary>
    public ObservableCollection<SynchFileItem> FileItems { get => fileItems; set => SetProperty(ref fileItems, value); }

    public SynchConfiguration SynchConfig { get; private set; }

    private PathInfo LocalRootPath => new PathInfo(Path.GetFullPath(SynchConfig.LocalPath)).Lock();
    private PathInfo RemoteRootPath => new PathInfo(SynchConfig.RemotePath).Lock();
    private PathInfo RemoteBackupPath => RemoteRootPath.Join(".backup").Lock();

    #endregion

    #region 命令
    public DelegateCommand<object?> SynchronizItemCommand { get; set; }
    public DelegateCommand<object?> RenameCommand { get; set; }
    public DelegateCommand<object?> DeleteCommand { get; set; }
    public DelegateCommand<object?> IgnoreCommand { get; set; }
    public DelegateCommand CreateDirCommand { get; set; }
    public DelegateCommand SynchronizAllCommand { get; set; }
    public DelegateCommand RefreshCommand { get; set; }

    #endregion

    public SynchFileViewModel(ICloudDriveProvider cloudDrive, ILogger<SynchFileViewModel> logger, ISnackbarMessageQueue snackbarMessageQueue, IRegionManager regionManager, IEventAggregator aggregator,AppConfiguration appConfiguration) : base(cloudDrive, logger, snackbarMessageQueue)
    {
        this.regionManager = regionManager;
        OperationItems = new List<GeneralListItem>()
        {
            new GeneralListItem() { Name = "配置同步", Icon = "CogSyncOutline",Command = new((obj) => aggregator.GetEvent<NavigateRequestEvent>().Publish("PreferencesView")) },
            new GeneralListItem() { Name = "刷新列表", Icon = "FolderSyncOutline" ,Command = new DelegateCommand<object?>( (obj)=> RefreshFileItems()) },
            new GeneralListItem() { Name = "全部同步", Icon = "FolderArrowUpDownOutline" ,Command = new ((obj)=> SynchronizAll()) }
        };
        SynchronizItemCommand = new(SynchronizItem);
        RenameCommand = new(RenameItem);
        DeleteCommand = new(DeleteItem);
        IgnoreCommand = new(IgnoreItem); ;
        CreateDirCommand = new(CreateDir); ;
        SynchronizAllCommand = new(SynchronizAll);
        RefreshCommand = new(RefreshFileItems);

        _ = aggregator.GetEvent<SynchConfigChangedEvent>().Subscribe(Init);
        SynchConfig = appConfiguration.SynchFileConfig;

        Init();
    }
    private void Init()
    {
        if (!autoSynchCts.IsCancellationRequested)
        {
            autoSynchCts.Cancel();
            autoSynchCts.Dispose();
        }
        autoSynchCts = new CancellationTokenSource();
        if (SynchConfig.AutoRefresh) AutoRefleshRoot();
        else RefreshFileItems(reload: true);
        if (SynchConfig.UseSchedule) _ = ScheduleStartAsync();
    }
    /// <summary>
    /// 自动同步
    /// </summary>
    private async void AutoRefleshRoot()
    {
        try
        {
            while (true)
            {
                RefreshFileItems(reload: true);
                await Task.Delay(SynchConfig.AutoRefreshSeconds * 1000, autoSynchCts.Token);
            }
        }
        catch (TaskCanceledException) { }
    }
    /// <summary>
    /// 开启定时任务
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private async Task ScheduleStartAsync(CancellationToken? token = null)
    {
        bool flag = false;//首次标记
        while (true)
        {
            if (token?.IsCancellationRequested ?? false) break;
            DateTime now = DateTime.Now;
            DateTime schedule = SynchConfig.Schedule;
            TimeSpan timeSpan;
            // 计算执行时间
            switch (SynchConfig.Frequency)
            {
                case SynchFrequency.Daily:
                    timeSpan = schedule.TimeOfDay - now.TimeOfDay;
                    if (timeSpan.TotalSeconds < 0) timeSpan.Add(new TimeSpan(24, 0, 0));
                    break;
                case SynchFrequency.Weekly:
                    timeSpan = new TimeSpan((7 + schedule.DayOfWeek - now.DayOfWeek) % 7, 0, 0, 0);
                    break;
                case SynchFrequency.Monthly:
                    schedule = schedule.AddYears(now.Year - schedule.Year).AddMonths(now.Month - schedule.Month); // 同年同月
                    timeSpan = schedule - now;
                    if (timeSpan.TotalSeconds < 0) timeSpan = schedule.AddMonths(1) - now;
                    break;
                default: throw new ArgumentOutOfRangeException("ScheduleStart 中存在未完成逻辑");
            }

            if (SynchConfig.UseSchedule && flag) RefreshFileItems(reload: true);
            flag = true;
            try
            {
                await Task.Delay(timeSpan, autoSynchCts.Token);
            }
            catch (TaskCanceledException) { break; }
        }
    }

    private async void CreateDir()
    {
        var dict = new Dictionary<string, string?>() { { "folder_name", null } };
        if (await DialogHostExtentions.ShowListDialogAsync(dict) && !string.IsNullOrEmpty(dict["folder_name"]))
        {
            var name = dict["folder_name"]!;
            DialogHostExtentions.ShowCircleProgressBar();
            var dir = Directory.CreateDirectory((string)LocalRootPath.Join(CurPath).Join(name));
            var res = await cloudDrive.UploadDirAsync((PathInfo)dir.FullName, RemoteRootPath.Join(CurPath));
            var isSuccess = res.All(e => e.IsSuccess);
            RefreshFileItems(true);
            DialogHostExtentions.CloseCircleProgressBar();
            snackbar.Enqueue(isSuccess ? "文件夹创建成功" : "文件夹创建失败");
        }
    }

    private void IgnoreItem(object? obj)
    {
        if (obj is not SynchFileItem itm) return;
        var node = root?.First(e => e?.Value?.Id == itm.Id);
        if (node != null)
            SynchConfig.Ignore.Paths.Add(node.Path.Replace(root!.Name, ""));
        RefreshFileItems();
    }

    private async void DeleteItem(object? obj)
    {
        if (obj is not SynchFileItem itm) return;
        DialogHostExtentions.ShowCircleProgressBar();
        if (itm.RemotePath != null)
        {
            var response = itm.IsDir ? await cloudDrive.DeleteDirAsync(itm.RemotePath) : await cloudDrive.DeleteAsync(itm.RemotePath);
            snackbar.Enqueue($"云盘文件 {itm.RemotePath} {(response.IsSuccess ? "删除成功" : "删除失败")}");
        }
        if (itm.LocalPath != null)
        {
            var isSuccess = false;
            try
            {
                if (itm.IsDir) Directory.Delete((string)itm.LocalPath, true);
                else File.Delete((string)itm.LocalPath);
                isSuccess = true;
            }
            catch { }
            finally { snackbar.Enqueue($"本地文件 {itm.LocalPath} {(isSuccess ? "删除成功" : "删除失败")}"); }
        }
        RefreshFileItems(true);
        DialogHostExtentions.CloseCircleProgressBar();
    }

    private async void RenameItem(object? obj)
    {
        if (obj is not SynchFileItem itm) return;
        var dict = new Dictionary<string, string?>() { { "folder_name", itm.Name } };
        if (!await DialogHostExtentions.ShowListDialogAsync(dict) || string.IsNullOrEmpty(dict["folder_name"])) return;
        var name = dict["folder_name"]!;
        // 检查重名
        if (FileItems.Any(e => e.Name == name))
        {
            _ = DialogHostExtentions.ShowMessageDialog($" 当前文件夹已存在名为 {name} 的文件");
            return;
        }
        DialogHostExtentions.ShowCircleProgressBar();
        if (itm.RemotePath != null)
        {
            var response = await cloudDrive.RenameAsync(itm.RemotePath, name);
            snackbar.Enqueue($"云盘文件 {itm.RemotePath} 重命名{(response.IsSuccess ? "成功" : "失败")}");
        }
        if (itm.LocalPath != null)
        {
            var isSuccess = false;
            try
            {
                var dest = Path.Join(itm.LocalPath.GetParentPath(), name);
                if (itm.IsDir) Directory.Move((string)itm.LocalPath, dest);
                else File.Move((string)itm.LocalPath, dest);
                isSuccess = true;
            }
            catch { }
            finally { snackbar.Enqueue($"本地文件 {itm.LocalPath} 重命名{(isSuccess ? "成功" : "失败")}"); }
        }
        RefreshFileItems(true);
        DialogHostExtentions.CloseCircleProgressBar();
    }
    private async void SynchronizItem(object? obj)
    {
        if (obj is not SynchFileItem itm) return;
        DialogHostExtentions.ShowCircleProgressBar();
        if (!(root?.TryGetNode(CurPath.Join(itm.Name).GetFullPath(), out Node<SynchFileItem>? node) ?? false)) return;
        await SynchronizNodeAsync(node!);
        RefreshFileItems(true);
        DialogHostExtentions.CloseCircleProgressBar();
    }

    /// <summary>
    /// 全部同步
    /// </summary>
    /// <returns></returns>
    private async void SynchronizAll()
    {
        if (root == null) return;
        DialogHostExtentions.ShowCircleProgressBar();
        foreach (var item in root.Children)
            await SynchronizNodeAsync(item);
        RefreshFileItems(true);
        DialogHostExtentions.CloseCircleProgressBar();
    }

    /// <summary>
    /// 同步节点及其子节点上的所有数据
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private async Task SynchronizNodeAsync(Node<SynchFileItem> node)
    {
        Stack<Node<SynchFileItem>> stack = new Stack<Node<SynchFileItem>>();
        stack.Push(node);
        while (stack.Count > 0)
        {
            Node<SynchFileItem> _node = stack.Pop();
            if (_node.Value is not SynchFileItem itm) continue;
            var sta = itm.State;
            // 新增，则上传文件/文件夹
            if (sta == SynchState.Added)
            {
                PathInfo relative = itm.LocalPath!.GetRelative(LocalRootPath);
                PathInfo remotePath = RemoteRootPath.Join(relative);
                if (itm.IsDir)
                    SnackbarMsg(await cloudDrive.UploadDirAsync(itm.LocalPath, (PathInfo)remotePath.GetParentPath()));
                else SnackbarMsg(await cloudDrive.UploadAsync(itm.LocalPath!, remotePath));
            }
            // 删除，软删除文件/整个文件夹（云端备份），同时删除子节点
            else if (sta == SynchState.Deleted)
            {
                PathInfo remoteBackupPath = RemoteBackupPath.Join(itm.RemotePath!.GetRelative(RemoteRootPath).GetFullPath() + DateTime.Now.ToLongDateString());
                string localPath = LocalRootPath.Join(itm.LocalPath!.GetRelative(LocalRootPath)).GetFullPath();
                ResponseMessage response = new(false) ;
                try
                {
                    response = await cloudDrive.MoveAsync(itm.RemotePath, remoteBackupPath); //移动备份
                    if (response.IsSuccess)
                    {
                        if (itm.IsDir) Directory.Delete(localPath);
                        else File.Delete(localPath);
                        _node.Children.Clear();
                    }
                }
                catch { }
                finally { snackbar.Enqueue($"{_node.Path}{(response.IsSuccess ? "同步成功" : "同步失败")}"); }
            }
            // 修改，备份文件后再上传
            else if (sta == SynchState.Modified)
            {
                PathInfo remoteBackupPath = RemoteBackupPath.Join(itm.RemotePath!.GetRelative(RemoteRootPath).GetFullPath() + DateTime.Now.ToLongDateString());
                var response = await cloudDrive.MoveAsync(itm.RemotePath, remoteBackupPath);
                if (response.IsSuccess)
                {
                    if (itm.IsDir) SnackbarMsg(await cloudDrive.UploadDirAsync(itm.LocalPath!, itm.RemotePath));
                    else SnackbarMsg(await cloudDrive.UploadAsync(itm.LocalPath!, itm.RemotePath));
                }
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
                    var res = await cloudDrive.DownloadAsync(itm.RemotePath!, (PathInfo)fileInfo.FullName);
                    if (res.IsSuccess) fileInfo.LastWriteTime = itm.RemoteUpdate ?? DateTime.Now;
                    SnackbarMsg(res, false);
                }
                // 如果是文件夹，创建文件夹并修改创建时间为云盘时间，将文件夹子节点入栈
                else
                {
                    DirectoryInfo info = Directory.CreateDirectory(localPath.GetFullPath());
                    info.LastAccessTime = (DateTime)itm.RemoteUpdate!;
                    foreach (var child in _node.Children) stack.Push(child);
                }
            }
            // 文件夹节点的子节点包含多种修改则先处理子节点，最后处理父节点
            else if (sta.HasFlag(SynchState.Added) || sta.HasFlag(SynchState.Deleted) || sta.HasFlag(SynchState.Modified))
            {
                stack.Push(_node);//当前节点入栈，待子节点处理完成最后处理
                foreach (var child in _node.Children) stack.Push(child); // 子节点依次入栈等待处理
            }
        }
    }

    private void SnackbarMsg(IEnumerable<ResponseMessage> results, bool upload = true)
    {
        foreach (var item in results) SnackbarMsg(item, upload);
    }
    private void SnackbarMsg(ResponseMessage result, bool upload = true)
    {
        snackbar.Enqueue(result.IsSuccess ? "同步成功" : $"同步失败{Environment.NewLine}errMsg:{result.ErrMessage}");
    }




    /// <summary>
    /// 根据本地文件和远程文件信息设置 FileItems
    /// </summary>
    /// <param name="reload">重载 root</param>
    protected async void RefreshFileItems(bool reload)
    {
        if (root == null || reload)
        {
            var remoteNode = await GetRemoteFileNodeAsync(RemoteRootPath, true);
            var localNode = FileUtils.GetLocalFileNode((string)LocalRootPath, true);
            root = SynchFileItem.CreateItemNode(localNode, remoteNode);
            var ignore = SynchConfig.Ignore;
            SynchFileItem.RefreshState(root, ignore);

        }
        var res = root?.GetNode((string)CurPath);
        if (res != null)
            FileItems = new ObservableCollection<SynchFileItem>(res.Children.Where(n => n.Value != null).Select(n => n.Value!).OrderByDescending(n => n.IsDir));
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
    private void SetAsyncConfig()
    {
        NavigationParameters keys = new NavigationParameters();
        keys.Add("title", "配置中心");
        regionManager.Regions["ContentRegion"].RequestNavigate("PreferencesView", keys);
    }


}
