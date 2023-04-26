using BDCloudDrive.Entities;
using CloudDriveUI.Configurations;
using CloudDriveUI.Models;
using CloudDriveUI.Views;
using DryIoc;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Options;
using Prism.Commands;
using Prism.Ioc;
using System.Threading.Tasks;
using System.Windows;

namespace CloudDriveUI.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private string title;
    private UserInfo? userInfo;
    public ISnackbarMessageQueue MainSnackbarMessageQueue { get; set; }

    public MainWindowViewModel(IRegionManager regionManager, ICloudDriveProvider cloudDrive, ISnackbarMessageQueue snackbarMessageQueue,AppConfiguration appConfiguration)
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
