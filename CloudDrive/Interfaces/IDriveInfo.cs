using CloudDrive.Entities;
using System.Threading.Tasks;

namespace CloudDrive.Interfaces;

public interface IDriveInfo
{
    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <returns></returns>
    public Task<UserInfo?> GetUserInfoAsync();
    /// <summary>
    /// 获取云盘信息
    /// </summary>
    /// <returns></returns>
    public Task<CloudDriveInfo?> GetDriveInfoAsync();
}
