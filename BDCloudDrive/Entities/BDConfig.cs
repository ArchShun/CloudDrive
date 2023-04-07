namespace BDCloudDrive.Entities;

/// <summary>
/// 百度配置
/// </summary>
/// <param name="AccessToken">鉴权参数</param>
/// <param name="Ondup">重复文件处理策略，fail(默认，直接返回失败)、newcopy(重命名文件)、overwrite、skip</param>
/// <param name="Rtype">创建文件冲突处理策略，0(默认，直接返回失败)、1(重命名文件)、2（path冲突且block_list不同才重命名）、3（覆盖）</param>
public record BDConfig
{
    public string? AccessToken { get; set; }
    public string? Ondup { get; set; }
    public int Rtype { get; set; } = 0;
}