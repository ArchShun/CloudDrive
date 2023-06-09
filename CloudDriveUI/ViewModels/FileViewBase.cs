﻿using CloudDriveUI.Core.Interfaces;
using CloudDriveUI.Domain;
using CloudDriveUI.Domain.Entities;
using CloudDriveUI.Models;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using System.Threading.Tasks;
using System.Windows;

namespace CloudDriveUI.ViewModels;

public abstract class FileViewBase<T> : BindableBase, IConfirmNavigationRequest where T : FileItemBase
{
    protected readonly ICloudDriveProvider cloudDrive;
    protected readonly ILogger logger;
    protected readonly ISnackbarMessage snackbar;
    protected readonly IFileItemService<T> itemService;
    private ObservableCollection<T> fileItems = new ObservableCollection<T>();


    /// <summary>
    /// 文件操作控件
    /// </summary>
    public List<GeneralListItem> OperationItems { get; init; } = new List<GeneralListItem>();
    /// <summary>
    /// 上下文菜单
    /// </summary>
    public List<GeneralListItem> ContextMenuItems { get; init; } = new List<GeneralListItem>();

    public FileViewBase(ICloudDriveProvider cloudDrive, ILogger logger, ISnackbarMessage snackbar, IFileItemService<T> itemService)
    {
        this.snackbar = snackbar;
        this.cloudDrive = cloudDrive;
        this.logger = logger;
        CurPath = new PathInfo();
        Title = "";

        RenameCommand = new(RenameItem);
        RefreshCommand = new(async () => await RefreshFileItemsAsync());
        DeleteCommand = new(async obj=>await DeleteItem(obj));

        OpenDirCommand = new(OpenDirAsync);
        NavDirCommand = new(NavDir);
        this.itemService = itemService;
    }


    #region 属性
    /// <summary>
    /// 正在加载中
    /// </summary>
    public bool IsLoading { get; set; } = false;

    /// <summary>
    /// 需要显示的文件列表
    /// </summary>
    public ObservableCollection<T> FileItems { get => fileItems; set => SetProperty(ref fileItems, value); }

    public string Title { get; private set; }
    /// <summary>
    /// 当前路径
    /// </summary>
    public PathInfo CurPath { get; set; }
    /// <summary>
    /// 双击文件夹展开
    /// </summary>
    public DelegateCommand<FileItemBase> OpenDirCommand { get; }

    /// <summary>
    /// 跳转文件夹
    /// </summary>
    public DelegateCommand<int?> NavDirCommand { get; }
    public DelegateCommand<object?> RenameCommand { get; set; }
    public DelegateCommand RefreshCommand { get; set; }
    public DelegateCommand<object?> DeleteCommand { get; set; }


    #endregion

    /// <summary>
    /// 刷新列表
    /// </summary>
    public async Task RefreshFileItemsAsync()
    {
        IsLoading = true;
        FileItems.Clear();
        IEnumerable<T> res = await itemService.Load(CurPath);
        FileItems.AddRange(res.OrderByDescending(n => n.IsDir));
        IsLoading = false;
    }

    public virtual void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 从其它页面跳转过来的时候调用
        if (navigationContext.Parameters.ContainsKey("title"))
        {
            Title = navigationContext.Parameters.GetValue<string>("title");
        }
    }

    public virtual bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public virtual void OnNavigatedFrom(NavigationContext navigationContext)
    {
        //throw new NotImplementedException();
    }

    public virtual void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
    {
        continuationCallback(navigationContext.Parameters.ContainsKey("title"));
    }

    private async void RenameItem(object? obj)
    {
        if (obj is not T itm) return;
        var listDialogItem = new FormItem("folder_name", itm.Name, new Predicate<string>(name => FileItems.All(e => e.Name != name)), "文件名已存在");
        var lst = new List<FormItem>() { listDialogItem };
        if (!await DialogHostExtentions.ShowListDialogAsync(lst) || listDialogItem.Value == itm.Name) return;
        var response = await itemService.Rename(itm, listDialogItem.Value);
        snackbar.Show($"{itm.Name} 重命名{(response.IsSuccess ? "成功" : "失败")}");
        _ = RefreshFileItemsAsync();
    }

    /// <summary>
    /// 文件路径导航
    /// </summary>
    /// <param name="i">路径 Paths 索引</param>
    protected void NavDir(int? i)
    {
        if (i is not int idx) return;
        var arr = CurPath.GetSegmentPath();
        CurPath = new PathInfo(arr[..idx]);
        _ = RefreshFileItemsAsync();
    }
    /// <summary>
    /// 打开文件夹
    /// </summary>
    /// <param name="itm">打开的列表项</param>
    protected void OpenDirAsync(FileItemBase itm)
    {
        if (!itm?.IsDir ?? false) return;
        var tmp = CurPath.Duplicate();
        try
        {
            CurPath.Join(itm!.Name, false);
            RaisePropertyChanged(nameof(CurPath));
            _ = RefreshFileItemsAsync();
        }
        catch (Exception ex)
        {
            CurPath = tmp;
            _ = RefreshFileItemsAsync();
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteItem(object? obj)
    {
        if (obj is not T itm) return;
        IsLoading = true;
        var response = await itemService.DeleteItem(itm);
        snackbar.Show($"云盘文件 {itm.Name} {(response.IsSuccess ? "删除成功" : "删除失败" + Environment.NewLine + response.ErrMessage)}");
        _ = RefreshFileItemsAsync();
        IsLoading = false;
    }

    public async Task CreateDir()
    {
        var dict = new List<FormItem>() { new FormItem("folder_name") };
        if (await DialogHostExtentions.ShowListDialogAsync(dict))
        {
            if (string.IsNullOrEmpty(dict[0].Value)) return;
            var name = dict[0].Value;
            IsLoading = true;
            var res = await itemService.CreateDir(CurPath.Join(name));
            _ = RefreshFileItemsAsync();
            IsLoading = false;
            snackbar.Show(res.IsSuccess ? "文件夹创建成功" : "文件夹创建失败");
        }
    }
}
