using CloudDriveUI.Views;
using Prism.Ioc;
using System.Windows;
using Microsoft.Extensions.Configuration;
using CloudDriveUI.Models;
using CloudDrive;
using BDCloudDrive;
using Prism.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloudDriveUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册类型
        containerRegistry.Register<NavigationBar>();

        // 注册导航
        containerRegistry.RegisterForNavigation<CloudFileView>();
        containerRegistry.RegisterForNavigation<SynchFileView>();
        
        containerRegistry.RegisterServices(service =>
        {
            // 注册云盘服务
            service.AddBDCloudDrive();

            // 注册配置项
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile("config.json", true, true);
            IConfigurationRoot root = builder.Build();
            service.AddOptions().Configure<AppConfig>(opt => root.Bind(opt));

            // 注册日志
            service.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            // 注册缓存
            service.AddMemoryCache();            
        });

    }
    protected virtual void LoadModuleCompleted(IModuleInfo moduleInfo, Exception error, bool isHandled)
    {

    }
}
