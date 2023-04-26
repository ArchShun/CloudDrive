using BDCloudDrive;
using CloudDrive.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudDriveTest;

[TestClass]

public class BDCloudDriveTest
{
    private readonly BDCloudDriveProvider _provider;

    public BDCloudDriveTest()
    {
        IMemoryCache memory = new MemoryCache(new MemoryCacheOptions());
        var logger = new Logger<BDCloudDriveProvider>(LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug)));
        _provider = new(memory, logger);
    }


    [TestMethod("删除文件夹")]
    public void TestDeleteDirAsync()
    {
        var path = new PathInfo("/apps/test/");
        var res = _provider.DeleteDirAsync(path).Result;
        Assert.IsTrue(res.IsSuccess);
    }

    [TestMethod("删除多个文件夹")]
    public void TestDeleteDirAsyncMutl()
    {
        var paths = new PathInfo[] { new PathInfo("/apps/test/loc_dir"), new PathInfo("/apps/test/no") };
        var res = _provider.DeleteDirAsync(paths).Result;
        Assert.IsTrue(res.IsSuccess);
    }

    [TestMethod("上传文件夹")]
    public void TestUploadDirAsync()
    {
        PathInfo path = (PathInfo)Path.GetFullPath("Test");
        PathInfo dest = (PathInfo)"/apps/test/";
        var res = _provider.UploadDirAsync(path, dest);
        Assert.IsTrue(res.Result.Any());
    }

    [TestMethod("递归获取文件信息")]
    public void TestGetFileListAllAsync()
    {
        var path = new PathInfo("/apps/test");
        var res = _provider.GetFileListAllAsync(path).Result;
        Assert.IsTrue(res.Any());
    }

    [TestMethod("移动文件")]
    public void TestMoveAsync()
    {
        var path = new PathInfo("/apps/test/__remote_lock__");
        var dest = new PathInfo("/apps/__remote_lock__");
        var res = _provider.MoveAsync(path, dest).Result;
        Assert.IsTrue(res.IsSuccess);
    }
    [TestMethod("移动文件夹")]
    public void TestMoveDirAsync()
    {
        var path = new PathInfo("/apps/test/");
        var dest = new PathInfo("/apps/test_move/test");
        var res = _provider.MoveAsync(path, dest).Result;
        Assert.IsTrue(res.IsSuccess);
    }

    [TestMethod("下载文件夹")]
    public void TestDownloadDirAsync()
    {
        PathInfo path = new PathInfo("/apps/test");
        PathInfo dest = (PathInfo)Path.GetFullPath("Test_download_dir");
        var res = _provider.DownloadDirAsync(path, dest).Result;
        foreach (var item in res)
        {
            Assert.IsTrue(item.IsSuccess);
        }
    }

    [TestMethod("下载文件")]
    public void TestDownloadAsync()
    {
        PathInfo path = new PathInfo("/apps/test/big.txt");
        PathInfo dest = (PathInfo)Path.GetFullPath(@"big.txt");
        var res = _provider.DownloadAsync(path, dest).Result;
        Assert.IsTrue(res.IsSuccess);
    }
}


