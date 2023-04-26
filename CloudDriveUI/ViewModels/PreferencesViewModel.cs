using CloudDriveUI.Configurations;
using CloudDriveUI.Models;
using Ookii.Dialogs.Wpf;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CloudDriveUI.ViewModels;

public class PreferencesViewModel : BindableBase
{
    private readonly IEventAggregator aggregator;

    private Login userLogin;
    private AppConfiguration appConfig;
    private string? _passwordValidated;

    public PreferencesViewModel(Login userLogin, AppConfiguration appConfig, IEventAggregator aggregator)
    {
        this.userLogin = userLogin;
        this.appConfig = appConfig;
        this.aggregator = aggregator;
        ModifyLocalPathCommand = new(ModifyLocalPath);
    }
    public DelegateCommand ModifyLocalPathCommand { get; set; }
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
