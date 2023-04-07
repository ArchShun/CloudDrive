using BDCloudDrive.Entities;
using DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Test;

ServiceCollection service = new ServiceCollection();

// 注册服务
service.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Trace);
})
    .AddMemoryCache()
    .AddBDCloudDrive();

// 注册配置项
ConfigurationBuilder builder = new ConfigurationBuilder();
builder.AddJsonFile("config.json", true, true);
IConfigurationRoot root = builder.Build();
service.AddOptions()
    .Configure<BDConfig>(opt => root.GetSection("BDConfig").Bind(opt));

// 注册程序集下的所有实现 TestControllerBase 的类
Assembly.GetExecutingAssembly().GetTypes().Where(type => typeof(TestControllerBase).IsAssignableFrom(type) && type != typeof(TestControllerBase))
    .ToList().ForEach(e => service.AddScoped(typeof(ITestController), e));


// 调用测试类
using ServiceProvider provider = service.BuildServiceProvider();
foreach (var itc in provider.GetServices<ITestController>())
{
    itc.Excute();
}




