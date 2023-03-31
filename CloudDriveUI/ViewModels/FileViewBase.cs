using CloudDrive.Interfaces;
using CloudDriveUI.Models;
using Prism.Commands;
using Prism.Regions;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;

namespace CloudDriveUI.ViewModels;

public class FileViewBase : BindableBase, IConfirmNavigationRequest
{

    protected readonly ICloudDriveProvider cloudDrive;
    private ObservableCollection<FileListItem> fileItems = new();

    public FileViewBase(ICloudDriveProvider cloudDrive)
    {
        this.cloudDrive = cloudDrive;

        OpenDirCommand = new(OpenDic);
        NavDirCommand = new(NavDir);

    }


    #region 属性

    /// <summary>
    /// 当前路径，第一项为界面标题："文件"
    /// </summary>
    public ObservableCollection<string> Paths { get; set; } = new ObservableCollection<string>() { "undefine" };
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
    public ObservableCollection<FileListItem> FileItems { get => fileItems; set => SetProperty(ref fileItems,value); }

    #endregion

    private void OpenDic(string id)
    {
        var itm = this.fileItems.First(e => e.Id == id);
        try
        {
            if (itm.IsDir)
            {
                Paths.Add(itm.Name ?? "");
                SetFileItemsAsync();
            }
        }
        catch (Exception ex)
        {
            Paths.RemoveAt(Paths.Count - 1);
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 文件路径导航
    /// </summary>
    /// <param name="i"></param>
    private void NavDir(int? i)
    {
        if (i != null)
        {
            while (Paths.Count > (int)i + 1)
                Paths.RemoveAt(Paths.Count - 1);
            SetFileItemsAsync();
        }
    }


    /// <summary>
    /// 根据当前路径获取文件列表，去除路径列表的第一项
    /// </summary>
    protected virtual void SetFileItemsAsync() { }


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
