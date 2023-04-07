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
        var res = await cloudDrive.CopyAsync(path, dest);
        Console.WriteLine(res);
        // { "errno":0,"info":[{ "errno":0,"path":"\/apps\/test\/\u6d4b\u8bd5\u6587\u672c.txt"}],"request_id":9063998206588864498}
    }
    //[TestMethod]
    public async Task TestCopyAsync2()
    {
        var path = "/apps/test/no/";
        var dest = "/apps/test/new_dir/";
        var res = await cloudDrive.CopyAsync(path, dest);
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
        var res = await cloudDrive.MoveAsync(path, dest);
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

    //[TestMethod]
    public async Task TestGetFileListAllAsync()
    {
        var list = await cloudDrive.GetFileListAllAsync("/apps/test/");
        foreach (var itm in list)
        {
            Console.WriteLine(JsonSerializer.Serialize(itm));
        }
    }


    [TestMethod]
    public async Task TestNode()
    {
        var remote_root = await GetRemoteFileNodeAsync("/apps/test", true, true);
        var local_root = FileUtils.GetLocalFileNode("E:\\test", true, true);

        var result = new ObservableCollection<FileListItem>();
        foreach (var itm in remote_root)
        {
            var info = local_root.FirstOrDefault(node => {
                Console.WriteLine(node?.Path + "----->" + itm.Path);
                return node?.Path == itm.Path;
            }, null);
            if (info?.Value != null && itm.Value != null) result.Add(new FileListItem(info.Value, itm.Value));
            else if (itm.Value != null) result.Add(new FileListItem(itm.Value));
        }
    }

    /// <summary>
    /// 获取云端文件节点
    /// </summary>
    /// <param name="path">根路径</param>
    /// <param name="recursion">是否递归</param>
    /// <param name="relocation">是否将根路径设置到 path </param>
    /// <returns></returns>
    private async Task<Node<CloudFileInfo>> GetRemoteFileNodeAsync(string path, bool recursion = false, bool relocation = true)
    {
        IEnumerable<CloudFileInfo> remInfos = new List<CloudFileInfo>();
        if (recursion) remInfos = await cloudDrive.GetFileListAllAsync(path);
        else remInfos = await cloudDrive.GetFileListAsync(path);
        var remote_root = new Node<CloudFileInfo>("remote_root");
        foreach (var itm in remInfos)
        {
            var node = new Node<CloudFileInfo>(itm.Name, itm);
            var paths = itm.Path.Split("/").Where(e => !string.IsNullOrEmpty(e)).ToArray();
            remote_root.Insert(node, paths);
        }
        if (relocation)
        {
            remote_root = remote_root.GetNode(path, '/');
            remote_root.Parent = null;
        }
        return remote_root;
    }

}
