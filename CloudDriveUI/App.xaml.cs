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
            else if( MessageBox.Show("授权失败，是否重试？","info",MessageBoxButton.OKCancel)==MessageBoxResult.Cancel) Environment.Exit(0);
        }
        return Container.Resolve<MainWindow>();
    }
    private readonly string confPath = "config.json";
    // 程序退出时保存修改的配置
    private readonly Dictionary<string, object?> toUpdateConfigs = new();
    private DelegateCommand? SaveConfigCommand;


    protected override void InitializeShell(Window shell)
    {
        base.InitializeShell(shell);
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册类型
        containerRegistry.Register<NavigationBar>();
        containerRegistry.RegisterSingleton<ISnackbarMessageQueue, SnackbarMessageQueue>();

        // 注册导航
        containerRegistry.RegisterForNavigation<CloudFileView>();
        containerRegistry.RegisterForNavigation<SynchFileView>();

        containerRegistry.RegisterServices(service =>
        {
            // 注册云盘服务
            service.AddBDCloudDrive();

            // 注册配置项
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile(confPath, true, true);
            IConfigurationRoot root = builder.Build();
            service.AddOptions().Configure<AppConfig>(opt => root.Bind(opt));
            // 退出程序时保存修改的配置
            SaveConfigCommand = new(() =>
            {
                var json = JsonNode.Parse(File.ReadAllText(confPath));
                if (json != null)
                {
                    var app = root.Get<AppConfig>() ?? new AppConfig();
                    foreach (var p in app.GetType().GetProperties())
                    {
                        toUpdateConfigs[p.Name] = p.GetValue(app);
                    }
                    foreach (var kv in toUpdateConfigs)
                    {
                        if (kv.Value != null)
                        {
                            json[kv.Key] = JsonNode.Parse(JsonSerializer.Serialize(kv.Value));
                        }
                    }
                    File.WriteAllText(confPath, json.ToJsonString(), Encoding.UTF8);
                }
            });
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
        SaveConfigCommand?.Execute();
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
        logger.LogError(e.Exception,e.Exception.Message);
    }
}
