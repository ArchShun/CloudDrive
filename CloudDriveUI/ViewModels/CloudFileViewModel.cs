using CloudDrive;
using CloudDrive.Interfaces;
using CloudDrive.Utils;
using CloudDriveUI.Models;
using Microsoft.Extensions.Logging;
using Ookii.Dialogs.Wpf;
using Prism.Commands;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CloudDriveUI.ViewModels;

public class CloudFileViewModel : FileViewBase
{

    protected ObservableCollection<CloudFileItem> fileItems = new();


    public CloudFileViewModel(ICloudDriveProvider cloudDrive, ILogger<CloudFileViewModel> logger) : base(cloudDrive, logger)
    {
        InitCommand();
        OperationItems = new List<OperationItem>()
        {
            new OperationItem() { Name = "上传文件", Icon = "FileUploadOutline",Command=new DelegateCommand<object?>(async (obj)=>await UploadFileAsync())},
            new OperationItem() { Name = "上传文件夹", Icon = "FolderUploadOutline" ,Command=new DelegateCommand<object?>(CreateDirectory)},
            new OperationItem() { Name = "新建文件夹", Icon = "FolderPlusOutline" ,Command=new DelegateCommand<object?>(CreateDirectory)},
            new OperationItem() { Name = "刷新列表", Icon = "CloudRefreshOutline" ,Command = new DelegateCommand<object?>(obj=>RefreshFileItems())},
         };
        RefreshFileItems();
    }

    #region 初始化命令
    private void InitCommand()
    {
        DownloadCommand = new(DownloadFileItem);
        RenameCommand = new(RenameFileItem);
        CreateDirCommand = new(CreateDirectory);
        UploadFileCommand = new(async (obj) => await UploadFileAsync());
        UploadDirCommand = new(async (obj) => await UploadDirAsync());
        RefreshCommand = new(obj => RefreshFileItems());
    }
    public DelegateCommand<object?> DownloadCommand { get; private set; }
    public DelegateCommand<object?> RenameCommand { get; private set; }
    public DelegateCommand<object?> CreateDirCommand { get; private set; }
    public DelegateCommand<object?> UploadFileCommand { get; private set; }
    public DelegateCommand<object?> UploadDirCommand { get; private set; }
    public DelegateCommand<object?> RefreshCommand { get; private set; }
    #endregion


    private async void RenameFileItem(object? obj)
    {
        if (obj is not CloudFileItem itm) return;
        Dictionary<string, string?> dict = new() { { "name", itm.Name } };
        if (await DialogHostExtentions.ShowListDialogAsync(dict))
        {
            DialogHostExtentions.ShowCircleProgressBar();
            var res = await cloudDrive.RenameAsync(CurPath.Duplicate().Join(itm.Name), dict["name"]!);
            await DialogHostExtentions.ShowMessageDialog(res ? "修改成功" : "修改失败");
            RefreshFileItems();
            DialogHostExtentions.CloseCircleProgressBar();
        }
    }

    private Task UploadDirAsync()
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<bool> UploadFileAsync()
    {
        var dialog = new VistaOpenFileDialog();
        dialog.Title = "打开文件";
        dialog.Multiselect = true;
        if (!(dialog.ShowDialog() ?? false)) return false;
        List<string> err = new();
        DialogHostExtentions.CloseCircleProgressBar();
        foreach (var name in dialog.FileNames)
        {
            var file = new PathInfo(name);
            var res = await cloudDrive.UploadAsync(file, CurPath.Duplicate().Join(file.GetName()));
            if (res == null) err.Add(name + "上传失败");
        }
        RefreshFileItems();
        DialogHostExtentions.CloseCircleProgressBar();
        await DialogHostExtentions.ShowMessageDialog((err.Count > 0) ? string.Join(Environment.NewLine, err) : "全部上传成功");
        return err.Count == 0;
    }

    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async void CreateDirectory(object? obj)
    {
        var dict = new Dictionary<string, string?>() { { "folder_name", null } };
        if (await DialogHostExtentions.ShowListDialogAsync(dict) && !string.IsNullOrEmpty(dict["folder_name"]))
        {
            string path = (string)CurPath.Join(dict["folder_name"]!);
            var res = await cloudDrive.CreateDirectoryAsync((PathInfo)path);
            if (res != null) FileItems.Add(new CloudFileItem(res));
            else await DialogHostExtentions.ShowMessageDialog($"文件夹{path}创建失败");
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
        var lst = await cloudDrive.GetFileListAllAsync((string)remotePath);
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
            await DialogHostExtentions.ShowMessageDialog((err.Count > 0) ? string.Join(Environment.NewLine, err) : "下载成功");
        }
    }

    /// <summary>
    /// 需要显示的文件列表
    /// </summary>
    public ObservableCollection<CloudFileItem> FileItems { get => fileItems; set => SetProperty(ref fileItems, value); }

    protected override async void RefreshFileItems()
    {
        var res = await cloudDrive.GetFileListAsync(CurPath);
        var items = res.Select(e => new CloudFileItem(e)).ToArray();
        FileItems = new ObservableCollection<CloudFileItem>(items);
    }
}


