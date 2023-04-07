using CloudDrive.Entities;
using System.Threading.Tasks;

namespace CloudDrive.Interfaces;

/// <summary>
/// 文件相关操作：移动、复制、重命名、上传、下载、新建文件夹
/// </summary>
public interface IFileManager
{
    /// <summary>
    /// 复制
    /// </summary>
    /// <param name="path">源路径</param>
    /// <param name="dest">目标路径</param>
    /// <returns>操作是否成功</returns>
    public Task<bool> CopyAsync(string path, string dest);
    /// <summary>
    /// 移动
    /// </summary>
    /// <param name="path">源路径</param>
    /// <param name="dest">目标路径</param>
    /// <returns>操作是否成功</returns>
    public Task<bool> MoveAsync(string path, string dest);
    /// <summary>
    /// 重命名
    /// </summary>
    /// <param name="path">源路径</param>
    /// <param name="name">目标路径</param>
    /// <returns>操作是否成功</returns>
    public Task<bool> RenameAsync(string path, string name);
    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="path">源路径</param>
    /// <returns>操作是否成功</returns>
    public Task<bool> DeleteAsync(string path);
    /// <summary>
    /// 上传
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="dest">上传到云盘路径</param>
    /// <returns></returns>
    public Task<CloudFileInfo?> UploadAsync(string path, string dest);

    /// <summary>
    /// 下载
    /// </summary>
    /// <param name="path">云盘文件路径</param>
    /// <param name="dest">保存到</param>
    /// <returns>是否成功</returns>
    public Task<bool> DownloadAsync(string path, string dest);

    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="path">文件夹路径</param>
    public Task<CloudFileInfo?> CreateDirectoryAsync(string path);
}
