using CloudDriveUI.Configurations;
using CloudDriveUI.Core.Interfaces;
using CloudDriveUI.Models;
using Prism.Commands;
using Prism.Events;

namespace CloudDriveUI.ViewModels;

public class PreferencesViewModel : BindableBase, INavigationAware
{
    private readonly IEventAggregator aggregator;
    private readonly IFolderBrowserDialog folderBrowserDialog;
    private Login userLogin;
    private AppConfiguration appConfig;
    private string? _passwordValidated;
    private int selectedIndex = 0;

    public PreferencesViewModel(Login userLogin, AppConfiguration appConfig, IEventAggregator aggregator,IFolderBrowserDialog folderBrowserDialog)
    {
        this.userLogin = userLogin;
        this.appConfig = appConfig;
        this.aggregator = aggregator;
        this.folderBrowserDialog = folderBrowserDialog;
        ModifyLocalPathCommand = new(ModifyLocalPath);
    }
    public DelegateCommand ModifyLocalPathCommand { get; set; }
    public int SelectedIndex
    {
        get => selectedIndex; set
        {
            selectedIndex = value;
            RaisePropertyChanged();
        }
    }
    public AppConfiguration AppConfig
    {
        get => appConfig; set
        {
            appConfig = value;
            RaisePropertyChanged();
        }
    }
    public Login UserLogin { get => userLogin; set { userLogin = value; RaisePropertyChanged(); } }
    private void ModifyLocalPath()
    {
        folderBrowserDialog.Description = "选择文件夹";
        folderBrowserDialog.Multiselect = false;
        if (folderBrowserDialog.ShowDialog() ?? false)
        {
            appConfig.SynchFileConfig.LocalPath = folderBrowserDialog.SelectedPath;
        }
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 从其它页面跳转过来的时候调用
        if (navigationContext.Parameters.ContainsKey("SelectedIndex") && int.TryParse(navigationContext.Parameters.GetValue<string>("SelectedIndex"), out int idx))
        {
            SelectedIndex = idx;
        }
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {

    }

    public string? PasswordValidated
    {
        get => _passwordValidated;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Password cannot be empty");
            SetProperty(ref _passwordValidated, value);
        }
    }

}
