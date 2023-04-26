using CloudDrive.Entities;
using System.Collections.Generic;
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
    public Task<ResponseMessage> CopyAsync(PathInfo path, PathInfo dest);
    /// <summary>
    /// 移动
    /// </summary>
    /// <param name="path">源路径</param>
    /// <param name="dest">目标路径</param>
    /// <returns>操作是否成功</returns>
    public Task<ResponseMessage> MoveAsync(PathInfo path, PathInfo dest);
    /// <summary>
    /// 重命名
    /// </summary>
    /// <param name="path">源路径</param>
    /// <param name="name">目标路径</param>
    /// <returns>操作是否成功</returns>
    public Task<ResponseMessage> RenameAsync(PathInfo path, string name);
    /// <summary>
    /// 删除单个文件
    /// </summary>
    /// <param name="path">源路径</param>
    /// <returns>操作是否成功</returns>
    public Task<ResponseMessage> DeleteAsync(PathInfo path);
    /// <summary>
    /// 删除多个文件
    /// </summary>
    /// <param name="files">文件路径列表</param>
    /// <returns>是否成功</returns>
    public Task<ResponseMessage> DeleteAsync(IEnumerable<PathInfo> files);

    /// <summary>
    /// 删除文件夹
    /// </summary>
    /// <param name="path">文件夹路径</param>
    /// <returns>删除是否成功</returns>
    Task<ResponseMessage> DeleteDirAsync(PathInfo path);
    /// <summary>
    /// 删除多个文件夹
    /// </summary>
    /// <param name="paths">文件夹路径</param>
    /// <returns>删除是否成功</returns>
    Task<ResponseMessage> DeleteDirAsync(IEnumerable<PathInfo> paths);

    /// <summary>
    /// 上传
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="dest">上传到云盘路径</param>
    /// <returns></returns>
    public Task<ResponseMessage> UploadAsync(PathInfo path, PathInfo dest);

    /// <summary>
    /// 下载
    /// </summary>
    /// <param name="path">云盘文件路径</param>
    /// <param name="dest">保存到</param>
    /// <returns>是否成功</returns>
    public Task<ResponseMessage> DownloadAsync(PathInfo path, PathInfo dest);
    /// <summary>
    /// 下载文件夹
    /// </summary>
    /// <param name="path">云端文件夹路径</param>
    /// <param name="dest">保存到本地路径</param>
    /// <returns></returns>
    public Task<IEnumerable<ResponseMessage>> DownloadDirAsync(PathInfo path, PathInfo dest);

    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="path">文件夹路径</param>
    public Task<ResponseMessage> CreateDirectoryAsync(PathInfo path);
    /// <summary>
    /// 上传文件夹
    /// </summary>
    /// <param name="src">本地文件夹</param>
    /// <param name="dest">目标文件夹</param>
    /// <returns>上传成功的文件</returns>
    public Task<IEnumerable<ResponseMessage>> UploadDirAsync(PathInfo src, PathInfo dest);
}
