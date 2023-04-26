using CloudDriveUI.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using System.Windows;

namespace CloudDriveUI.ViewModels;

public abstract class FileViewBase : BindableBase, IConfirmNavigationRequest
{

    protected readonly ICloudDriveProvider cloudDrive;
    protected readonly ILogger logger;
    protected readonly ISnackbarMessageQueue snackbar;

    private PathInfo curPath;


    /// <summary>
    /// 文件操作控件
    /// </summary>
    public List<GeneralListItem> OperationItems { get; init; } = new List<GeneralListItem>();
    /// <summary>
    /// 上下文菜单
    /// </summary>
    public List<GeneralListItem> ContextMenuItems { get; init; } = new List<GeneralListItem>();

    public FileViewBase(ICloudDriveProvider cloudDrive, ILogger logger, ISnackbarMessageQueue snackbarMessageQueue)
    {
        snackbar = snackbarMessageQueue;
        this.cloudDrive = cloudDrive;
        this.logger = logger;
        curPath = new PathInfo();
        Title = "";

        OpenDirCommand = new(OpenDirAsync);
        NavDirCommand = new(NavDir);
    }


    #region 属性
    public string Title { get; private set; }
    /// <summary>
    /// 当前路径
    /// </summary>
    public PathInfo CurPath { get => curPath; set => SetProperty(ref curPath, value); }
    /// <summary>
    /// 双击文件夹展开
    /// </summary>
    public DelegateCommand<FileItemBase> OpenDirCommand { get; }

    /// <summary>
    /// 跳转文件夹
    /// </summary>
    public DelegateCommand<int?> NavDirCommand { get; }


    #endregion

    /// <summary>
    /// 刷新列表
    /// </summary>
    protected abstract void RefreshFileItems();

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


    /// <summary>
    /// 文件路径导航
    /// </summary>
    /// <param name="i">路径 Paths 索引</param>
    protected void NavDir(int? i)
    {
        if (i is not int idx) return;
        var arr = CurPath.GetFullPath().Split(CurPath.Separator);
        CurPath = new PathInfo(arr[..idx]);
        RefreshFileItems();
    }
    /// <summary>
    /// 打开文件夹
    /// </summary>
    /// <param name="itm">打开的列表项</param>
    protected void OpenDirAsync(FileItemBase itm)
    {
        if (!itm?.IsDir??false) return;
        var tmp = CurPath.Duplicate();
        try
        {
            CurPath.Join(itm!.Name,false);
            RaisePropertyChanged(nameof(CurPath));
            RefreshFileItems();
        }
        catch (Exception ex)
        {
            CurPath = tmp;
            RefreshFileItems();
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
