namespace BDCloudDrive.Entities;

/// <summary>
/// 文件信息响应数据
/// </summary>
/// <param name="Fs_Id">文件在云端的唯一标识ID</param>
/// <param name="Path">文件的绝对路径</param>
/// <param name="Server_Filename">文件名称</param>
/// <param name="Size">文件大小，单位B</param>
/// <param name="Server_Mtime">文件在服务器创建时间</param>
/// <param name="Server_Ctime">文件在服务器创建时间</param>
/// <param name="Local_Mtime">文件在客户端修改时间</param>
/// <param name="Local_Ctime">文件在客户端创建时间</param>
/// <param name="Md5">云端哈希（非文件真实MD5），只有是文件类型时，该字段才存在</param>
/// <param name="Dir_Empty">该目录是否存在子目录，只有请求参数web=1且该条目为目录时，该字段才存在， 0为存在， 1为不存在</param>
/// <param name="Category">文件类型，1 视频、2 音频、3 图片、4 文档、5 应用、6 其他、7 种子</param>
/// <param name="IsDir">是否为目录，0 文件、1 目录</param>
/// <param name="Thumbs">只有请求参数web=1且该条目分类为图片时，该字段才存在，包含三个尺寸的缩略图URL</param>
public record FileInfoResult(long Fs_Id, string Path, string Server_Filename, long Size, long Server_Mtime, long Server_Ctime, long Local_Mtime, long Local_Ctime, string? Md5, int? Dir_Empty, int Category, int IsDir, object? Thumbs);
