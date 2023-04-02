using BDCloudDrive.Entities;
using CloudDriveUI.Models;
using Microsoft.Extensions.Options;
using Prism.Commands;
using System.Threading.Tasks;
using System.Windows;

namespace CloudDriveUI.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private List<OperationItem> operationItems;
    private string title;
    private UserInfo? userInfo;

    public MainWindowViewModel(IRegionManager regionManager, ICloudDriveProvider cloudDrive)
    {
        title = "CloudDrive";
        operationItems = new List<OperationItem>()
        {
            new OperationItem() { Name = "个人中心", Icon = "AccountCogOutline" }
        };
        regionManager.RegisterViewWithRegion("NavigateRegion", "NavigationBar");

        UserInfo = Task.Run(cloudDrive.GetUserInfoAsync).Result;
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

    public UserInfo? UserInfo
    {
        get
        {
            return userInfo;
        }
        private set { userInfo = value; RaisePropertyChanged(); }
    }

}
