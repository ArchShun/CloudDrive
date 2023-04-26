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
        OperationItems = new List<GeneralListItem>()
        {
            new GeneralListItem() { Name = "上传文件", Icon = "FileUploadOutline",Command=new ( obj=>UploadFileAsync())},
            new GeneralListItem() { Name = "上传文件夹", Icon = "FolderUploadOutline" ,Command=new DelegateCommand<object?>(obj=>UploadDirAsync())},
            new GeneralListItem() { Name = "新建文件夹", Icon = "FolderPlusOutline" ,Command=new DelegateCommand<object?>(obj=>CreateDirectory())},
            new GeneralListItem() { Name = "刷新列表", Icon = "CloudRefreshOutline" ,Command = new DelegateCommand<object?>(obj=>RefreshFileItems())},
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
        if (res.IsSuccess) RefreshFileItems();
        DialogHostExtentions.CloseCircleProgressBar();
        snackbar.Enqueue(res.IsSuccess ? "删除成功" : "删除失败");
    }
    /// <summary>
    /// 重命名
    /// </summary>
    /// <param name="obj"></param>
    private async void RenameFileItem(object? obj)
    {
        if (obj is not CloudFileItem itm) return;
        Dictionary<string, string?> dict = new() { { "name", itm.Name } };
        if (!await DialogHostExtentions.ShowListDialogAsync(dict)) return;
        string name = dict["name"]!;
        if (FileItems.Any(e => e.Name == name))
        {
            snackbar.Enqueue($"{name} 已存在");
            return;
        }
        DialogHostExtentions.ShowCircleProgressBar();
        var res = await cloudDrive.RenameAsync(CurPath.Duplicate().Join(itm.Name), name);
        RefreshFileItems();
        await Task.Delay(TimeSpan.FromSeconds(2));
        DialogHostExtentions.CloseCircleProgressBar();
        snackbar.Enqueue(res.IsSuccess ? "修改成功" : "修改失败");
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
            var res = await cloudDrive.UploadDirAsync((PathInfo)select_dir, CurPath.Duplicate());
            DialogHostExtentions.CloseCircleProgressBar();
            foreach (var item in res)
                snackbar.Enqueue(item.IsSuccess ? "上传成功" : $"上传失败{Environment.NewLine}errMsg:{item.ErrMessage}");
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
            ResponseMessage res = await cloudDrive.CreateDirectoryAsync(path);
            DialogHostExtentions.CloseCircleProgressBar();
            snackbar.Enqueue(res.IsSuccess ? "文件夹创建成功" : "文件夹创建失败");
            if (res.IsSuccess) RefreshFileItems();
        }
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
            PathInfo localPath = (PathInfo)FileUtils.LocalPathDupPolicy(Path.Join(dialog.SelectedPath, itm.Name));
            PathInfo remotePath = CurPath.Join(itm.Name);
            DialogHostExtentions.ShowCircleProgressBar();
            if (!itm.IsDir)
            {
                var res = await cloudDrive.DownloadAsync(remotePath, localPath);
                snackbar.Enqueue(res.IsSuccess ? "下载成功" : $"下载失败{Environment.NewLine}errMsg:{res.ErrMessage}");
            }
            else
            {
                var res = await cloudDrive.DownloadDirAsync(remotePath, localPath);
                foreach (var msg in res)
                    snackbar.Enqueue(msg.IsSuccess ? "下载成功" : $"下载失败{Environment.NewLine}errMsg:{msg.ErrMessage}");
            }
            DialogHostExtentions.CloseCircleProgressBar();
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


