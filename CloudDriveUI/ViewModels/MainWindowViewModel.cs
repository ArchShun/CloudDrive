using CloudDriveUI.Configurations;
using System.Threading.Tasks;

namespace CloudDriveUI.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private string title;
    private UserInfo? userInfo;

    public MainWindowViewModel(IRegionManager regionManager, ICloudDriveProvider cloudDrive, AppConfiguration appConfiguration)
    {
        title = "CloudDrive";
        regionManager.RegisterViewWithRegion("NavigateRegion", "NavigationBar");

        UserInfo = Task.Run(cloudDrive.GetUserInfoAsync).Result;

    }
    public string Title
    {
        get { return title; }
        set { title = value; RaisePropertyChanged(); }
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
