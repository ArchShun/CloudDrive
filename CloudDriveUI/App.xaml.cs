using CloudDriveUI.Views;
using Prism.Ioc;
using System.Windows;
using Microsoft.Extensions.Configuration;
using CloudDriveUI.Models;
using Microsoft.Extensions.DependencyInjection;
using DependencyInjection;
using Microsoft.Extensions.Logging;
using BDCloudDrive.Entities;
using Prism.Commands;
using System.Text.Json.Nodes;
using System.Text;

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
    private readonly string confPath = "config.json";
    // 程序退出时保存修改的配置
    private readonly Dictionary<string, object?> toUpdateConfigs = new();
    private DelegateCommand? SaveConfigCommand;

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
            builder.AddJsonFile(confPath, true, true);
            IConfigurationRoot root = builder.Build();
            service.AddOptions()
                .Configure<AppConfig>(opt => root.Bind(opt))
                .Configure<BDConfig>(opt =>
                {
                    root.GetSection("BDConfig").Bind(opt);
                    toUpdateConfigs.Add("BDConfig", opt);
                });
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
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
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
}
