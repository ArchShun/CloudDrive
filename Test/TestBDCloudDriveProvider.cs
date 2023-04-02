using BDCloudDrive.Entities;
using CloudDrive.Interfaces;
using Microsoft.Extensions.Options;

namespace Test;

internal class TestBDCloudDriveProvider : TestControllerBase
{
    private readonly ICloudDriveProvider cloudDrive;
    private readonly IOptionsSnapshot<BDConfig> options;

    public TestBDCloudDriveProvider(ICloudDriveProvider provider, IOptionsSnapshot<BDConfig> options)
    {
        cloudDrive = provider;
        this.options = options;
    }

    [TestMethod]
    public async Task TestUserInfoAsync()
    {
        var info = await cloudDrive.GetUserInfoAsync();
        Console.WriteLine(info);
    }

    //[TestMethod]
    public async Task TestDownloadAsync()
    {
        await cloudDrive.DownloadAsync("/apps/test/download_test.txt", "download_test.txt");
    }

    //[TestMethod]
    public async Task TestUploadAsync()
    {
        await cloudDrive.UploadAsync("upload_test.txt", " /apps/test/upload_test.txt");
        await cloudDrive.UploadAsync("上传中文路径测试.txt", " /apps/test/上传中文路径测试.txt");
    }

    //[TestMethod]
    //public async Task Test()
    //{
    //    Console.WriteLine("-----------------");
    //    HttpClient client = new();
    //    var AccessToken = options.Value.AccessToken;
    //    var url = $"https://pan.baidu.com/rest/2.0/xpan/nas?access_token={AccessToken}&method=uinfo";
    //    var response = client.GetAsync(url).Result;
    //    var str = response.Content.ReadAsStringAsync().Result;
    //    Console.WriteLine(str);
    //}

    //[TestMethod]
    public async Task Test()
    {
        Console.WriteLine("-----------------");
        HttpClient client = new();
        var AccessToken = options.Value.AccessToken;
        var url = $"https://pan.baidu.com/rest/2.0/xpan/nas?access_token={AccessToken}&method=uinfo";
        var response = await client.GetAsync(url);
        var str = await response.Content.ReadAsStringAsync();
        Console.WriteLine(str);
    }



}
