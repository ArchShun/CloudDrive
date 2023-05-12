using CloudDrive.Utils;
using CloudDriveUI.Core.Interfaces;
using CloudDriveUI.Domain;
using CloudDriveUI.Domain.Entities;
using CloudDriveUI.Models;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using System.Threading.Tasks;

namespace CloudDriveUI.ViewModels;

public class CloudFileViewModel : FileViewBase<CloudFileItem>
{
    private readonly ISelectFileDialog selectFileDialog;
    private readonly IFolderBrowserDialog folderBrowserDialog;
    private new readonly CloudFileItemService itemService;

    #region 命令
    public DelegateCommand<object?> DownloadCommand { get; private set; }
    public DelegateCommand CreateDirCommand { get; private set; }
    public DelegateCommand UploadFileCommand { get; private set; }
    public DelegateCommand UploadDirCommand { get; private set; }
    #endregion

    public CloudFileViewModel(ICloudDriveProvider cloudDrive, ILogger<CloudFileViewModel> logger, ISelectFileDialog selectFileDialog, ISnackbarMessage snackbar, IFolderBrowserDialog folderBrowserDialog, CloudFileItemService itemService) : base(cloudDrive, logger, snackbar, itemService)
    {
        DownloadCommand = new(DownloadFileItem);
        CreateDirCommand = new(CreateDirectory);
        UploadFileCommand = new(async () => await UploadFileAsync());
        UploadDirCommand = new(async () => await UploadDirAsync());
        OperationItems = new List<GeneralListItem>()
        {
            new GeneralListItem() { Name = "上传文件", Icon = "FileUploadOutline",Command=new (async (obj)=>await UploadFileAsync())},
            new GeneralListItem() { Name = "上传文件夹", Icon = "FolderUploadOutline" ,Command=new DelegateCommand<object?>(async obj=>await UploadDirAsync())},
            new GeneralListItem() { Name = "新建文件夹", Icon = "FolderPlusOutline" ,Command=new DelegateCommand<object?>(obj=>CreateDirectory())},
            new GeneralListItem() { Name = "刷新列表", Icon = "CloudRefreshOutline" ,Command = new DelegateCommand<object?>(async obj=>await RefreshFileItemsAsync())},
         };
        _ = RefreshFileItemsAsync();
        this.selectFileDialog = selectFileDialog;
        this.folderBrowserDialog = folderBrowserDialog;
        this.itemService = itemService;
    }
    private async Task UploadDirAsync()
    {
        folderBrowserDialog.Multiselect = true;
        if (!(folderBrowserDialog.ShowDialog() ?? false)) return;
        // 遍历选择的文件夹
        IsLoading = true;
        foreach (var select_dir in folderBrowserDialog.SelectedPaths)
        {
            var res = await cloudDrive.UploadDirAsync((PathInfo)select_dir, CurPath.Duplicate());
            foreach (var item in res)
                snackbar.Show(item.IsSuccess ? "上传成功" : $"上传失败{Environment.NewLine}errMsg:{item.ErrMessage}");
        }
        _ = RefreshFileItemsAsync();
        IsLoading = false;
    }
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task UploadFileAsync()
    {
        selectFileDialog.Title = "打开文件";
        selectFileDialog.Multiselect = true;
        if (!(selectFileDialog.ShowDialog() ?? false)) return;
        IsLoading = true;
        List<string> err = new();
        foreach (var name in selectFileDialog.FileNames)
        {
            var file = new PathInfo(name);
            var res = await cloudDrive.UploadAsync(file, CurPath.Duplicate().Join(file.GetName()));
            if (res == null) err.Add(name + "上传失败");
        }
        _ = RefreshFileItemsAsync();
        snackbar.Show((err.Count > 0) ? string.Join(Environment.NewLine, err) : "全部上传成功");
        IsLoading = false;
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
        IsLoading = true;
        string name = dict[0].Value;
        ResponseMessage res = await itemService.CreateDir(CurPath.Duplicate().Join(name));
        snackbar.Show(res.IsSuccess ? "文件夹创建成功" : "文件夹创建失败");
        if (res.IsSuccess) _ = RefreshFileItemsAsync();
        IsLoading = false;
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async void DownloadFileItem(object? obj)
    {
        if (obj is not CloudFileItem itm) return;
        folderBrowserDialog.Description = "保存到";
        folderBrowserDialog.Multiselect = false;
        if (folderBrowserDialog.ShowDialog() ?? false)
        {
            IsLoading = true;
            PathInfo localPath = (PathInfo)FileUtils.LocalPathDupPolicy(Path.Join(folderBrowserDialog.SelectedPath, itm.Name));
            PathInfo remotePath = CurPath.Join(itm.Name);
            if (!itm.IsDir)
            {
                var res = await cloudDrive.DownloadAsync(remotePath, localPath);
                snackbar.Show(res.IsSuccess ? "下载成功" : $"下载失败{Environment.NewLine}errMsg:{res.ErrMessage}");
            }
            else
            {
                var res = await cloudDrive.DownloadDirAsync(remotePath, localPath);
                foreach (var msg in res)
                    snackbar.Show(msg.IsSuccess ? "下载成功" : $"下载失败{Environment.NewLine}errMsg:{msg.ErrMessage}");
            }
            IsLoading = false;
        }
    }

}


