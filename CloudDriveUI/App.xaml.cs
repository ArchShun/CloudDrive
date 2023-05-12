using CloudDriveUI.Configurations;
using CloudDriveUI.Core.Interfaces;
using CloudDriveUI.Domain;
using CloudDriveUI.Models;
using CloudDriveUI.Views;
using DependencyInjection;
using DryIoc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;

namespace CloudDriveUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication
{
    protected override Window CreateShell()
    {
        var drive = Container.Resolve<ICloudDriveProvider>();
        while (true)
        {
            if (drive.Authorize()) break;
            else if (MessageBox.Show("授权失败，是否重试？", "info", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) Environment.Exit(0);
        }
        return Container.Resolve<MainWindow>();
    }


    protected override void InitializeShell(Window shell)
    {
        base.InitializeShell(shell);
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册类型
        containerRegistry.Register<NavigationBar>();
        containerRegistry.RegisterScoped<ISnackbarMessage, SnackbarMessage>();
        containerRegistry.RegisterScoped<IFolderBrowserDialog, FolderBrowserDialog>();
        containerRegistry.RegisterScoped<ISelectFileDialog, SelectFileDialog>();
        containerRegistry.Register<Login>();
        containerRegistry.Register<CloudFileItemService>();
        containerRegistry.Register<SynchFileItemService>();
        containerRegistry.RegisterSingleton<AppConfiguration>();

        ConfigurationBuilder builder = new ConfigurationBuilder();
        builder.AddJsonFile("config.json", true, true);
        IConfigurationRoot root = builder.Build();

        // 注册导航
        containerRegistry.RegisterForNavigation<CloudFileView>();
        containerRegistry.RegisterForNavigation<SynchFileView>();
        containerRegistry.RegisterForNavigation<PreferencesView>();

        containerRegistry.RegisterServices(service =>
        {
            // 注册云盘服务
            //service.AddBDCloudDrive();
            service.AddMockCloudDrive();

            // 注册日志
            service.AddLogging(builder =>
            {
                builder.AddNLog();
            });

            // 注册缓存
            service.AddMemoryCache();

        });
    }
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var logger = Container.Resolve<ILogger<App>>();
        logger.LogError(e.Exception, e.Exception.Message);
    }
}
