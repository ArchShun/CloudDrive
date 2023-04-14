using System.Security.AccessControl;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BDCloudDrive.Entities;

/// <summary>
/// 百度配置
/// </summary>
/// <param name="AccessToken">鉴权参数</param>
/// <param name="Ondup">重复文件处理策略，fail(默认，直接返回失败)、newcopy(重命名文件)、overwrite、skip</param>
/// <param name="Rtype">创建文件冲突处理策略，0(默认，直接返回失败)、1(重命名文件)、2（path冲突且block_list不同才重命名）、3（覆盖）</param>
public record BDConfig
{
    private static readonly string path = "bd_config.json";
    public BDConfig() { }
    /// <summary>
    /// 读取配置
    /// </summary>
    /// <returns></returns>
    public static BDConfig Create()
    {
        FileInfo file = new(path);
        using var fs = file.Create(FileMode.OpenOrCreate, FileSystemRights.Read | FileSystemRights.Write, FileShare.None, 1024, FileOptions.Encrypted, null);
        var res = new BDConfig();
        try
        {
            res = JsonSerializer.Deserialize<BDConfig>(fs) ?? new BDConfig();
        }
        catch { }
        return res;
    }
    /// <summary>
    /// 保存配置
    /// </summary>
    public void Save()
    {
        FileInfo file = new(path);
        using var fs = file.Create(FileMode.OpenOrCreate, FileSystemRights.Read | FileSystemRights.Write, FileShare.None, 1024, FileOptions.Encrypted, null);
        JsonSerializer.Serialize(fs, this);
    }

    public string AccessToken { get; set; } = string.Empty;
    public string Ondup { get; set; } = "fail";
    public int Rtype { get; set; } = 0;
}