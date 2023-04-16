using CloudDrive.Utils;
using CloudDriveUI.Models;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using System.Threading.Tasks;
using Ookii.Dialogs.Wpf;
using MaterialDesignThemes.Wpf;
using System.Xml.Linq;

namespace CloudDriveUI.ViewModels;

public class CloudFileViewModel : FileViewBase
{

    private ObservableCollection<CloudFileItem> fileItems = new();

    /// <summary>
    /// 需要显示的文件列表
    /// </summary>
    public ObservableCollection<CloudFileItem> FileItems { get => fileItems; set => SetProperty(ref fileItems, value); }
    #region 命令
    public DelegateCommand<object?> DownloadCommand { get; private set; }
    public DelegateCommand<object?> RenameCommand { get; private set; }
    public DelegateCommand<object?> DeleteCommand { get; private set; }
    public DelegateCommand CreateDirCommand { get; private set; }
    public DelegateCommand UploadFileCommand { get; private set; }
    public DelegateCommand UploadDirCommand { get; private set; }
    public DelegateCommand<object?> RefreshCommand { get; private set; }
    #endregion



    public CloudFileViewModel(ICloudDriveProvider cloudDrive, ILogger<CloudFileViewModel> logger, ISnackbarMessageQueue snackbarMessageQueue) : base(cloudDrive, logger, snackbarMessageQueue)
    {

        DownloadCommand = new(DownloadFileItem);
        RenameCommand = new(RenameFileItem);
        CreateDirCommand = new(CreateDirectory);
        UploadFileCommand = new(UploadFileAsync);
        UploadDirCommand = new(UploadDirAsync);
        RefreshCommand = new(obj => RefreshFileItems());
        DeleteCommand = new(DeleteItem);
        OperationItems = new List<OperationItem>()
        {
            new OperationItem() { Name = "上传文件", Icon = "FileUploadOutline",Command=new ( obj=>UploadFileAsync())},
            new OperationItem() { Name = "上传文件夹", Icon = "FolderUploadOutline" ,Command=new DelegateCommand<object?>(obj=>UploadDirAsync())},
            new OperationItem() { Name = "新建文件夹", Icon = "FolderPlusOutline" ,Command=new DelegateCommand<object?>(obj=>CreateDirectory())},
            new OperationItem() { Name = "刷新列表", Icon = "CloudRefreshOutline" ,Command = new DelegateCommand<object?>(obj=>RefreshFileItems())},
         };
        RefreshFileItems();
    }

    /// <summary>
    /// 删除文件项
    /// </summary>
    /// <param name="obj"></param>
    private async void DeleteItem(object? obj)
    {
        if (obj is not CloudFileItem itm) return;
        DialogHostExtentions.ShowCircleProgressBar();
        var path = CurPath.Duplicate().Join(itm.Name);
        var res = itm.IsDir ? await cloudDrive.DeleteDirAsync(path) : await cloudDrive.DeleteAsync(path);
        DialogHostExtentions.CloseCircleProgressBar();
        if (res) RefreshFileItems();
        snackbar.Enqueue(res ? "删除成功" : "删除失败", null, null, null, false, true, TimeSpan.FromSeconds(2));
    }
    /// <summary>
    /// 重命名
    /// </summary>
    /// <param name="obj"></param>
    private async void RenameFileItem(object? obj)
    {
        if (obj is not CloudFileItem itm) return;
        Dictionary<string, string?> dict = new() { { "name", itm.Name } };
        if (await DialogHostExtentions.ShowListDialogAsync(dict))
        {
            DialogHostExtentions.ShowCircleProgressBar();
            var res = await cloudDrive.RenameAsync(CurPath.Duplicate().Join(itm.Name), dict["name"]!);
            RefreshFileItems();
            DialogHostExtentions.CloseCircleProgressBar();
            snackbar.Enqueue(res ? "修改成功" : "修改失败", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }
    }

    private async void UploadDirAsync()
    {
        var dialog = new VistaFolderBrowserDialog();
        dialog.Multiselect = true;
        if (!(dialog.ShowDialog() ?? false)) return;
        // 遍历选择的文件夹
        foreach (var select_dir in dialog.SelectedPaths)
        {
            DialogHostExtentions.ShowCircleProgressBar();
            IEnumerable<CloudFileInfo> res = await cloudDrive.UploadDirAsync((PathInfo)select_dir, CurPath.Duplicate());
            DialogHostExtentions.CloseCircleProgressBar();
            snackbar.Enqueue(res.Any() ? $"{select_dir}中的{res.Count()}个文件上传成功" : "上传失败", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }
        RefreshFileItems();
    }
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async void UploadFileAsync()
    {
        var dialog = new VistaOpenFileDialog();
        dialog.Title = "打开文件";
        dialog.Multiselect = true;
        if (!(dialog.ShowDialog() ?? false)) return;
        List<string> err = new();
        DialogHostExtentions.ShowCircleProgressBar();
        foreach (var name in dialog.FileNames)
        {
            var file = new PathInfo(name);
            var res = await cloudDrive.UploadAsync(file, CurPath.Duplicate().Join(file.GetName()));
            if (res == null) err.Add(name + "上传失败");
        }
        RefreshFileItems();
        DialogHostExtentions.CloseCircleProgressBar();
        snackbar.Enqueue((err.Count > 0) ? string.Join(Environment.NewLine, err) : "全部上传成功", null, null, null, false, true, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async void CreateDirectory()
    {

        var dict = new Dictionary<string, string?>() { { "folder_name", null } };
        if (await DialogHostExtentions.ShowListDialogAsync(dict) && !string.IsNullOrEmpty(dict["folder_name"]))
        {
            var path = CurPath.Duplicate().Join(dict["folder_name"]!);
            DialogHostExtentions.ShowCircleProgressBar();
            var res = await cloudDrive.CreateDirectoryAsync(path);
            DialogHostExtentions.CloseCircleProgressBar();
            if (res != null) FileItems.Add(new CloudFileItem(res));
            snackbar.Enqueue(res != null ? "文件夹创建成功" : "文件夹创建失败", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }
    }

    /// <summary>
    /// 下载文件夹
    /// </summary>
    /// <param name="remotePath"></param>
    /// <param name="localPath"></param>
    /// <returns></returns>
    private async Task<List<string>> DownloadDirectoryAsync(PathInfo remotePath, PathInfo localPath)
    {
        var errs = new List<string>();
        localPath = (PathInfo)Path.GetFullPath((string)localPath);
        var lst = await cloudDrive.GetFileListAllAsync(remotePath);
        if (lst != null) foreach (var info in lst)
            {
                var relative = info.Path.GetRelative(remotePath); // 获取相对路径
                var local = localPath.Duplicate().Join((string)relative); // 拼接本地路径
                if (info.IsDir)
                    Directory.CreateDirectory((string)local);
                else
                {
                    var res = await cloudDrive.DownloadAsync(info.Path, local);
                    if (!res) errs.Add($"{info.Path}");
                }
            }
        return errs;
    }


    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async void DownloadFileItem(object? obj)
    {
        if (obj is not CloudFileItem itm) return;
        var dialog = new VistaFolderBrowserDialog
        {
            UseDescriptionForTitle = true,
            Description = "保存到",
            Multiselect = false,
        };
        if (dialog.ShowDialog() ?? false)
        {
            var err = new List<string>();
            PathInfo localPath = (PathInfo)FileUtils.LocalPathDupPolicy(Path.Join(dialog.SelectedPath, itm.Name));
            PathInfo remotePath = CurPath.Join(itm.Name);
            DialogHostExtentions.ShowCircleProgressBar();
            if (!itm.IsDir)
            {
                var res = await cloudDrive.DownloadAsync(remotePath, localPath);
                if (!res) err.Add((string)remotePath);
            }
            else
            {
                var res = await DownloadDirectoryAsync(remotePath, localPath);
                err.AddRange(res);
            }
            DialogHostExtentions.CloseCircleProgressBar();
            snackbar.Enqueue((err.Count > 0) ? string.Join(Environment.NewLine, err) : "下载成功", null, null, null, false, true, TimeSpan.FromSeconds(2));
        }
    }


    protected override async void RefreshFileItems()
    {
        DialogHostExtentions.ShowCircleProgressBar();
        var res = await cloudDrive.GetFileListAsync(CurPath);
        var items = res.Select(e => new CloudFileItem(e)).ToArray();
        FileItems = new ObservableCollection<CloudFileItem>(items);
        DialogHostExtentions.CloseCircleProgressBar();
    }
}


