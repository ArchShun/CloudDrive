using CloudDrive.Entities;

namespace CloudDrive.Interfaces;

public interface IDriveInfo
{
    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <returns></returns>
    public UserInfo? GetUserInfo();
    /// <summary>
    /// 获取云盘信息
    /// </summary>
    /// <returns></returns>
    public CloudDriveInfo? GetDriveInfo();
}
