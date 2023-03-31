using CloudDriveUI.Models;
using Microsoft.Extensions.Options;
using Prism.Commands;
using System.Windows;

namespace CloudDriveUI.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private List<OperationItem> operationItems;
    private string title;
    private UserInfo? userInfo;

    public MainWindowViewModel(IRegionManager regionManager, ICloudDrive cloudDrive,IOptionsSnapshot<AppConfig> options)
    {
        title = "CloudDrive";
        operationItems = new List<OperationItem>()
        {
            new OperationItem() { Name = "个人中心", Icon = "AccountCogOutline" }
        };
        regionManager.RegisterViewWithRegion("NavigateRegion", "NavigationBar");

        userInfo = cloudDrive.GetUserInfo();

        // 设置保存配置命令
        AppCommands.SaveConfigCommand.RegisterCommand(new DelegateCommand(()=> options.Value.SaveAsync()));

    }

    public string Title
    {
        get { return title; }
        set { title = value; RaisePropertyChanged(); }
    }


    public List<OperationItem> OperationItems
    {
        get { return operationItems; }
        set { operationItems = value; RaisePropertyChanged(); }
    }

    public UserInfo? UserInfo { get => userInfo; private set { userInfo = value; RaisePropertyChanged(); } }

}
