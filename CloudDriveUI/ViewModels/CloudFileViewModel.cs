using CloudDrive.Utils;
using CloudDriveUI.Domain;
using CloudDriveUI.Domain.Entities;
using CloudDriveUI.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Ookii.Dialogs.Wpf;
using Prism.Commands;

namespace CloudDriveUI.ViewModels;

public class CloudFileViewModel : FileViewBase<CloudFileItem>
{
    private new readonly CloudFileItemService itemService;

    #region 命令
    public DelegateCommand<object?> DownloadCommand { get; private set; }
    public DelegateCommand CreateDirCommand { get; private set; }
    public DelegateCommand UploadFileCommand { get; private set; }
    public DelegateCommand UploadDirCommand { get; private set; }
    #endregion

    public CloudFileViewModel(ICloudDriveProvider cloudDrive, ILogger<CloudFileViewModel> logger, ISnackbarMessageQueue snackbarMessageQueue, CloudFileItemService itemService) : base(cloudDrive, logger, snackbarMessageQueue, itemService)
    {
        DownloadCommand = new(DownloadFileItem);
        CreateDirCommand = new(CreateDirectory);
        UploadFileCommand = new(UploadFileAsync);
        UploadDirCommand = new(UploadDirAsync);
        OperationItems = new List<GeneralListItem>()
        {
            new GeneralListItem() { Name = "上传文件", Icon = "FileUploadOutline",Command=new ( obj=>UploadFileAsync())},
            new GeneralListItem() { Name = "上传文件夹", Icon = "FolderUploadOutline" ,Command=new DelegateCommand<object?>(obj=>UploadDirAsync())},
            new GeneralListItem() { Name = "新建文件夹", Icon = "FolderPlusOutline" ,Command=new DelegateCommand<object?>(obj=>CreateDirectory())},
            new GeneralListItem() { Name = "刷新列表", Icon = "CloudRefreshOutline" ,Command = new DelegateCommand<object?>(async obj=>await RefreshFileItemsAsync())},
         };
        _ = RefreshFileItemsAsync();
        this.itemService = itemService;
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
        _ = RefreshFileItemsAsync();
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
        _ = RefreshFileItemsAsync();
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
        var dict = new List<FormItem>() { new FormItem("folder_name") };
        if (!await DialogHostExtentions.ShowListDialogAsync(dict) || string.IsNullOrEmpty(dict[0].Value)) return;
        string name = dict[0].Value;
        DialogHostExtentions.ShowCircleProgressBar();
        ResponseMessage res = await itemService.CreateDir(CurPath.Duplicate().Join(name));
        DialogHostExtentions.CloseCircleProgressBar();
        snackbar.Enqueue(res.IsSuccess ? "文件夹创建成功" : "文件夹创建失败");
        if (res.IsSuccess) _ = RefreshFileItemsAsync();
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

}


