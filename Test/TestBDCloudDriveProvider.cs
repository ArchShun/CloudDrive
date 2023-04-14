using BDCloudDrive;
using BDCloudDrive.Entities;
using CloudDrive.Entities;
using CloudDrive.Interfaces;
using CloudDrive.Utils;
using CloudDriveUI.Models;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Xml.Linq;

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

    //[TestMethod]
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
        await cloudDrive.UploadAsync("上传中文路径测试.txt", " /apps/test/上传中文路径测试.txt");
    }


    //[TestMethod]
    public async Task TestGetDriveInfoAsync()
    {
        var info = await cloudDrive.GetDriveInfoAsync();
        Console.WriteLine(info);
    }

    //[TestMethod]
    public async Task TestCreateDirectory()
    {
        var path = "/apps/test/newpath2";
        var res = await cloudDrive.CreateDirectoryAsync(path);
        Console.WriteLine(res);
    }

    //[TestMethod]
    public async Task TestCopyAsync1()
    {
        var path = "/apps/test/测试文本.txt";
        var dest = "/apps/test/测试文本-复制.txt";
        var res = await cloudDrive.CopyAsync(dest, dest);
        Console.WriteLine(res);
        // { "errno":0,"info":[{ "errno":0,"path":"\/apps\/test\/\u6d4b\u8bd5\u6587\u672c.txt"}],"request_id":9063998206588864498}
    }
    //[TestMethod]
    public async Task TestCopyAsync2()
    {
        var path = "/apps/test/no/";
        var dest = "/apps/test/new_dir/";
        var res = await cloudDrive.CopyAsync(dest, dest);
        Console.WriteLine(res);
        Console.WriteLine("-------------------");

        //path = "/apps/test/测试文本.txt";
        //dest = "/apps/test/move_test/";
        //res = await cloudDrive.CopyAsync(path, dest);
        //Console.WriteLine(res);
        //Console.WriteLine("-------------------");

        //path = "/apps/test/move_test/";
        //dest = "/apps/test/move_test/1.txt";
        //res = await cloudDrive.CopyAsync(path, dest);
        //Console.WriteLine(res);
        //Console.WriteLine("-------------------");

    }

    //[TestMethod]
    public async Task TestMoveAsync()
    {
        var path = "/apps/test/no/";
        var dest = "/apps/test/new_dir/";
        var res = await cloudDrive.MoveAsync(dest, dest);
        Console.WriteLine(res);
    }

    //[TestMethod]
    public async Task TestRenameAsync()
    {
        var path = "/apps/test/move_test_dir/测试文本-复制.txt";
        var name = "测试文本-复制-重命名.txt";
        var res = await cloudDrive.RenameAsync(path, name);
        Console.WriteLine(res);
    }

    //[TestMethod]
    public async Task TestRenameAsync2()
    {
        var path = "/apps/test/new_dir3/";
        var name = "new_dir/new_dir3";
        var res = await cloudDrive.RenameAsync(path, name);
        Console.WriteLine(res);
    }

    //[TestMethod]
    public async Task TestDeleteAsync()
    {
        var path = "/apps/test/new_dir/测试文本-移动.txt";
        var res = await cloudDrive.DeleteAsync(path);
        Console.WriteLine(res);
    }

    [TestMethod]
    public async Task TestGetFileListAllAsync()
    {
        var list = await cloudDrive.GetFileListAllAsync("/apps/test/");
        foreach (var itm in list)
        {
            Console.WriteLine(JsonSerializer.Serialize(itm));
        }
    }


}
