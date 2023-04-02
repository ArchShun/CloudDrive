using CloudDrive.Interfaces;
using System.Text.Json;
using System.Threading.Channels;

namespace Test;

internal class TestBDCloudDriveProvider : TestControllerBase
{
    private readonly ICloudDriveProvider cloudDrive;

    public TestBDCloudDriveProvider(ICloudDriveProvider provider)
    {
        cloudDrive = provider;
    }

    //[TestMethod]
    //public async Task TestUserInfoAsync()
    //{
    //    var info = await cloudDrive.GetUserInfoAsync();
    //    Console.WriteLine(info);
    //}

    //[TestMethod]
    public async Task TestDownloadAsync()
    {
        await cloudDrive.DownloadAsync("/apps/test/download_test.txt", "download_test.txt");
    }

    [TestMethod]
    public async Task TestUploadAsync()
    {
        await cloudDrive.UploadAsync("upload_test.txt", " /apps/test/upload_test.txt");
        await cloudDrive.UploadAsync("上传中文路径测试.txt", " /apps/test/上传中文路径测试.txt");
    }




}
