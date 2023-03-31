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
    /// <param name="recursion">是否递归</param>
    /// <param name="type">文件类型过滤</param>
    /// <returns></returns>
    public Task<IEnumerable<CloudFileInfo>> GetFileListAsync(string path, Dictionary<string, object>? options = null);
    /// <summary>
    /// 获取文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns></returns>
    public Task<CloudFileInfo?> GetFileInfoAsync(string path);
    /// <summary>
    /// 递归获取所有文件信息
    /// </summary>
    /// <returns></returns>
    public IEnumerable<CloudFileInfo> GetFileAll();

}
