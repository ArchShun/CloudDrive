using CloudDriveUI.Configurations;
using CloudDriveUI.Models;
using Ookii.Dialogs.Wpf;
using Prism.Commands;
using Prism.Events;

namespace CloudDriveUI.ViewModels;

public class PreferencesViewModel : BindableBase, INavigationAware
{
    private readonly IEventAggregator aggregator;

    private Login userLogin;
    private AppConfiguration appConfig;
    private string? _passwordValidated;
    private int selectedIndex = 0;

    public PreferencesViewModel(Login userLogin, AppConfiguration appConfig, IEventAggregator aggregator)
    {
        this.userLogin = userLogin;
        this.appConfig = appConfig;
        this.aggregator = aggregator;
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
        var dialog = new VistaFolderBrowserDialog
        {
            UseDescriptionForTitle = true,
            Description = "选择文件夹",
            Multiselect = false,
        };
        if (dialog.ShowDialog() ?? false)
        {
            appConfig.SynchFileConfig.LocalPath = dialog.SelectedPath;
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
