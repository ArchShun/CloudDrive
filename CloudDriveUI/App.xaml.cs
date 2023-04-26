using CloudDriveUI.Views;
using Prism.Ioc;
using System.Windows;
using Microsoft.Extensions.Configuration;
using CloudDriveUI.Models;
using Microsoft.Extensions.DependencyInjection;
using DependencyInjection;
using Prism.Commands;
using System.Text.Json.Nodes;
using System.Text;
using NLog.Extensions.Logging;
using DryIoc;
using Microsoft.Extensions.Logging;
using CloudDriveUI.ViewModels;
using MaterialDesignThemes.Wpf;
using Prism.DryIoc.Extensions;
using CloudDriveUI.Configurations;

namespace CloudDriveUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
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
        containerRegistry.RegisterSingleton<ISnackbarMessageQueue>(() => new SnackbarMessageQueue(TimeSpan.FromSeconds(1)));
        containerRegistry.Register<Login>();
        //if (AppConfiguration.Load() is AppConfiguration tmp)
        //    containerRegistry.RegisterInstance(tmp);
        //else 
            containerRegistry.RegisterSingleton<AppConfiguration>();

        // 注册导航
        containerRegistry.RegisterForNavigation<CloudFileView>();
        containerRegistry.RegisterForNavigation<SynchFileView>();
        containerRegistry.RegisterForNavigation<PreferencesView>();

        containerRegistry.RegisterServices(service =>
        {
            // 注册云盘服务
            service.AddBDCloudDrive();
            //service.AddMockCloudDrive();

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
