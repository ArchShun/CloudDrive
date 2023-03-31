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
    /// <param name="src">源路径</param>
    /// <param name="dest">目标路径</param>
    /// <returns></returns>
    public CloudFileInfo? Copy(string src, string dest);
    /// <summary>
    /// 移动
    /// </summary>
    /// <param name="src">源路径</param>
    /// <param name="dest">目标路径</param>
    /// <returns></returns>
    public CloudFileInfo? Remove(string src, string dest);
    /// <summary>
    /// 重命名
    /// </summary>
    /// <param name="src">源路径</param>
    /// <param name="dest">目标路径</param>
    /// <returns></returns>
    public CloudFileInfo? Rename(string src, string dest);
    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="src">源路径</param>
    public void Delete(string src);
    /// <summary>
    /// 上传
    /// </summary>
    /// <param name="src">文件路径</param>
    /// <param name="dest">上传到云盘路径</param>
    /// <returns></returns>
    public Task<CloudFileInfo?> UploadAsync(string src, string dest);

    /// <summary>
    /// 下载
    /// </summary>
    /// <param name="src">云盘文件路径</param>
    /// <param name="dest">保存到</param>
    /// <returns>是否成功</returns>
    public Task<bool> DownloadAsync(string src, string dest);

    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="path">文件夹路径</param>
    public void CreateDirectory(string path);
}
