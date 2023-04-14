using BDCloudDrive;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CloudDriveUI.ViewModels;
using System.Reflection;

namespace CloudDriveTest;
[TestClass]
public class CloudFileViewModelTest
{
    private readonly CloudFileViewModel vm;
    public CloudFileViewModelTest()
    {
        IMemoryCache memory = new MemoryCache(new MemoryCacheOptions());
        BDCloudDriveProvider bd = new(memory, new Logger<BDCloudDriveProvider>(LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug))));
        vm = new CloudFileViewModel(bd, new Logger<CloudFileViewModel>(LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug))));
    }
    private T? Invoke<T>(string methodName, params object?[] _params) where T : class
    {
        var m = vm.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var result = m?.Invoke(vm, _params);
        return result as T;
    }
    [TestMethod("文件上传")]
    public void UploadFileAsyncTest()
    {
        var res = Invoke<Task<bool>>("UploadFileAsync", "");
        Assert.IsTrue(res?.Result);
    }

}
