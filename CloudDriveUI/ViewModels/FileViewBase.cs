using CloudDrive.Interfaces;
using CloudDriveUI.Models;
using Prism.Commands;
using Prism.Regions;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace CloudDriveUI.ViewModels;

public abstract class FileViewBase : BindableBase, IConfirmNavigationRequest
{

    protected readonly ICloudDriveProvider cloudDrive;
    protected ObservableCollection<FileListItem> fileItems = new();
    private ObservableCollection<string> paths = new ObservableCollection<string>() { "undefine" };

    public FileViewBase(ICloudDriveProvider cloudDrive)
    {
        this.cloudDrive = cloudDrive;

        OpenDirCommand = new(OpenDir);
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
    public DelegateCommand<string> OpenDirCommand { get; }

    /// <summary>
    /// 跳转文件夹
    /// </summary>
    public DelegateCommand<int?> NavDirCommand { get; }
    /// <summary>
    /// 需要显示的文件列表
    /// </summary>
    public ObservableCollection<FileListItem> FileItems { get => fileItems; set => SetProperty(ref fileItems, value); }

    #endregion

    /// <summary>
    /// 打开文件夹
    /// </summary>
    /// <param name="id">FilListItem Id</param>
    protected abstract void OpenDir(string id);

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
