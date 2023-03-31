using CloudDrive.Entities;

namespace CloudDrive.Interfaces;

public interface ICloudDriveProvider : IFileInfo,IFileManager,IDriveInfo
{
    public UserInfo? UserInfo { get; set; }
    public CloudDriveInfo? DriveInfo { get; set; }

}
