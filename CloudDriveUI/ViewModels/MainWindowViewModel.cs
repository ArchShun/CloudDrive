using CloudDriveUI.Configurations;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;

namespace CloudDriveUI.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private string title;
    private UserInfo? userInfo;
    public ISnackbarMessageQueue MainSnackbarMessageQueue { get; set; }

    public MainWindowViewModel(IRegionManager regionManager, ICloudDriveProvider cloudDrive, ISnackbarMessageQueue snackbarMessageQueue, AppConfiguration appConfiguration)
    {
        title = "CloudDrive";
        regionManager.RegisterViewWithRegion("NavigateRegion", "NavigationBar");

        UserInfo = Task.Run(() => cloudDrive.GetUserInfoAsync()).Result;

        MainSnackbarMessageQueue = snackbarMessageQueue;
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
