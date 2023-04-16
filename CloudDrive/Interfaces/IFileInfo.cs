using CloudDrive.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudDrive.Interfaces;

public interface IFileInfo
{


    /// <summary>
    /// 获取文件列表
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns></returns>
    public Task<IEnumerable<CloudFileInfo>> GetFileListAsync(PathInfo path);
    /// <summary>
    /// 获取文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns></returns>
    public Task<CloudFileInfo?> GetFileInfoAsync(PathInfo path);
    /// <summary>
    /// 递归获取所有文件信息
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns></returns>
    public Task<IEnumerable<CloudFileInfo>> GetFileListAllAsync(PathInfo path);

}
