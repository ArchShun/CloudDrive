using CloudDrive.Entities;
using CloudDrive.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MockCloudDrive;

public class MockCloudDriveProvider : ICloudDriveProvider, IDisposable
{
    private readonly ResponseMessage response = new(true, "Mock Err Message");
    private readonly IEnumerable<ResponseMessage> transmitResults = new List<ResponseMessage>();
    private readonly CloudDriveInfo cloudDriveInfo = new(1L << 32, 1L << 16, 1L << 16);
    private readonly List<CloudFileInfo> cloudFileInfos = new()
    {
        new CloudFileInfo(){ Id=new Random().NextInt64(), Category=FileType.Document, IsDir=true, LocalCtime = DateTime.Now.Second, LocalMtime = DateTime.Now.Second, ServerCtime = DateTime.Now.Ticks, Path = (PathInfo)"/apps/test" , Name="test", Size=12044},
        new CloudFileInfo(){ Id=new Random().NextInt64(), Category=FileType.Document, IsDir=true, LocalCtime = DateTime.Now.Second, LocalMtime = DateTime.Now.Second, ServerCtime = DateTime.Now.Second, Path = (PathInfo)"/apps/test2" , Name="test2", Size=12044},
        new CloudFileInfo(){ Id=new Random().NextInt64(), Category=FileType.Document, IsDir=false, LocalCtime = DateTime.Now.Second, LocalMtime = DateTime.Now.Second, ServerCtime = DateTime.Now.Second, Path = (PathInfo)"/apps/test2/test3" , Name="test3", Size=12044},
        new CloudFileInfo(){ Id=new Random().NextInt64(), Category=FileType.Document, IsDir=false, LocalCtime = DateTime.Now.Second, LocalMtime = DateTime.Now.Second, ServerCtime = DateTime.Now.Second, Path = (PathInfo)"/apps/test2/test4" , Name="test4", Size=12044},
        new CloudFileInfo(){ Id=new Random().NextInt64(), Category=FileType.Document, IsDir=false, LocalCtime = DateTime.Now.Second, LocalMtime = DateTime.Now.Second, ServerCtime = DateTime.Now.Second, Path = (PathInfo)"/apps/test5" , Name="test5", Size=12044},
        new CloudFileInfo(){ Id=new Random().NextInt64(), Category=FileType.Document, IsDir=false, LocalCtime = DateTime.Now.Second, LocalMtime = DateTime.Now.Second, ServerCtime = DateTime.Now.Second, Path = (PathInfo)"/apps/test6" , Name="test6", Size=12044}
    };
    public bool Authorize()
    {
        return true;
    }

    public Task<ResponseMessage> CopyAsync(PathInfo path, PathInfo dest)
    {
        return Task.FromResult(response);
    }


    public Task<ResponseMessage> DeleteAsync(PathInfo path)
    {
        return Task.FromResult(response);
    }

    public Task<ResponseMessage> DeleteAsync(IEnumerable<PathInfo> files)
    {
        return Task.FromResult(response);
    }

    public Task<ResponseMessage> DeleteDirAsync(PathInfo path)
    {
        return Task.FromResult(response);
    }

    public Task<ResponseMessage> DeleteDirAsync(IEnumerable<PathInfo> paths)
    {
        return Task.FromResult(response);
    }

    public void Dispose()
    {

    }

    public Task<ResponseMessage> DownloadAsync(PathInfo path, PathInfo dest)
    {
        return Task.FromResult(response);
    }

    public Task<IEnumerable<ResponseMessage>> DownloadDirAsync(PathInfo path, PathInfo dest)
    {
        IEnumerable<ResponseMessage> lst = new List<ResponseMessage>() { response };
        return Task.FromResult(lst);
    }

    public Task<CloudDriveInfo?> GetDriveInfoAsync()
    {
        return Task.FromResult<CloudDriveInfo?>(cloudDriveInfo);
    }

    public Task<CloudFileInfo?> GetFileInfoAsync(PathInfo path)
    {
        return Task.FromResult<CloudFileInfo?>(cloudFileInfos[0]);
    }

    public Task<IEnumerable<CloudFileInfo>> GetFileListAllAsync(PathInfo path)
    {
        return Task.FromResult<IEnumerable<CloudFileInfo>>(cloudFileInfos);

    }

    public Task<IEnumerable<CloudFileInfo>> GetFileListAsync(PathInfo path)
    {
        return Task.FromResult<IEnumerable<CloudFileInfo>>(cloudFileInfos);
    }

    public Task<UserInfo?> GetUserInfoAsync()
    {
        return Task.FromResult<UserInfo?>(null);
    }

    public Task<ResponseMessage> MoveAsync(PathInfo path, PathInfo dest)
    {
        return Task.FromResult(response);
    }

    public Task<ResponseMessage> RenameAsync(PathInfo path, string name)
    {
        return Task.FromResult(response);
    }

    Task<UploadResponseMessage> IFileManager.CreateDirectoryAsync(PathInfo path)
    {
        return Task.Delay(1000).ContinueWith((t) => new UploadResponseMessage(true));
    }

    Task<UploadResponseMessage> IFileManager.UploadAsync(PathInfo path, PathInfo dest)
    {
        return Task.Delay(1000).ContinueWith((t) => new UploadResponseMessage(true));
    }

    Task<IEnumerable<UploadResponseMessage>> IFileManager.UploadDirAsync(PathInfo src, PathInfo dest)
    {
        return Task.Delay(1000).ContinueWith((t) => Enumerable.Range(0, 5).Select((i) => new UploadResponseMessage(true))); ;
    }
}
