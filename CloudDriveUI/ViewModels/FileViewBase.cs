using CloudDriveUI.Models;
using Prism.Commands;

namespace CloudDriveUI.ViewModels;

public abstract class FileViewBase : BindableBase, IConfirmNavigationRequest
{

    protected readonly ICloudDriveProvider cloudDrive;
    private ObservableCollection<string> paths = new ObservableCollection<string>() { "undefine" };

    public FileViewBase(ICloudDriveProvider cloudDrive)
    {
        this.cloudDrive = cloudDrive;

        OpenDirCommand = new(OpenDirAsync);
        NavDirCommand = new(NavDir);
    }


    #region 属性

    /// <summary>
    /// 当前路径，第一项为界面标题："文件"
    /// </summary>
    public ObservableCollection<string> Paths { get => paths; set => SetProperty(ref paths, value); }
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
    /// 打开文件夹
    /// </summary>
    /// <param name="itm">打开的列表项</param>
    protected abstract void OpenDirAsync(FileItemBase itm);

    /// <summary>
    /// 文件路径导航
    /// </summary>
    /// <param name="i">路径 Paths 索引</param>
    protected abstract void NavDir(int? i);

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 从其它页面跳转过来的时候调用
        if (navigationContext.Parameters.ContainsKey("title"))
        {
            Paths[0] = navigationContext.Parameters.GetValue<string>("title");
        }
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        //throw new NotImplementedException();
    }

    public void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
    {
        continuationCallback(navigationContext.Parameters.ContainsKey("title"));
    }
}
