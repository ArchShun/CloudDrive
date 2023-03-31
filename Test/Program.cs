using DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Test;

ServiceCollection services = new ServiceCollection();

// 注册服务
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Trace);
})
    .AddMemoryCache()
    .AddBDCloudDrive();

// 注册程序集下的所有实现 TestControllerBase 的类
Assembly.GetExecutingAssembly().GetTypes().Where(type => typeof(TestControllerBase).IsAssignableFrom(type) && type != typeof(TestControllerBase))
    .ToList().ForEach(e => services.AddScoped(typeof(ITestController), e));


// 调用测试类
using ServiceProvider provider = services.BuildServiceProvider();
provider.GetRequiredService<ITestController>().Excute();




