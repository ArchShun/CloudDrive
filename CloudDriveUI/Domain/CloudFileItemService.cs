using CloudDriveUI.Configurations;
using CloudDriveUI.Domain.Entities;
using System.Threading.Tasks;

namespace CloudDriveUI.Domain;

public class CloudFileItemService : IFileItemService<CloudFileItem>
{
    private readonly AppConfiguration _conf;
    private readonly ICloudDriveProvider cloudDrive;
    private PathInfo GetRemoteBackupPath(PathInfo RemotePath)
    {
        return new PathInfo(".backup").Join(RemotePath + "." + DateTime.Now.ToString("yyyyMMddHHmmss"), false);
    }
    public CloudFileItemService(ICloudDriveProvider cloudDrive, AppConfiguration conf)
    {
        this.cloudDrive = cloudDrive;
        _conf = conf;
    }



    public Task<ResponseMessage> DeleteItem(CloudFileItem item)
    {
        return cloudDrive.MoveAsync(item.RemotePath, GetRemoteBackupPath(item.RemotePath));
    }

    public async Task<IEnumerable<CloudFileItem>> Load(PathInfo relativePath)
    {
        try
        {
            var res = await cloudDrive.GetFileListAsync(relativePath);
            return res.Select(e => new CloudFileItem(e));
        }
        catch { }
        return new List<CloudFileItem>();
    }

    public Task<ResponseMessage> Rename(CloudFileItem item, string name)
    {
        return cloudDrive.RenameAsync(item.RemotePath, name);
    }

    public Task<UploadResponseMessage> CreateDir(PathInfo relativePath)
    {
        return cloudDrive.CreateDirectoryAsync(relativePath.Join(relativePath));
    }
}
