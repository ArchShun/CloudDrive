namespace CloudDriveUI.Models;

public class SynchIgnore
{
    private List<string> extensions = new();
    private List<string> names = new();
    private List<string> paths = new();

    /// <summary>
    /// 忽略指定路径下的所有文件
    /// </summary>
    public List<string> Paths { get => paths; set => paths = value; }
    /// <summary>
    /// 忽略指定的文件名称
    /// </summary>
    public List<string> Names { get => names; set => names = value; }
    /// <summary>
    /// 忽略指定的扩展名
    /// </summary>
    public List<string> Extensions { get => extensions.Select(ext => ext.StartsWith('.') ? ext : ("." + ext)).ToList(); set => extensions = value; }

    /// <summary>
    /// 检查是否被忽略
    /// </summary>
    /// <param name="path"></param>
    /// <returns>true 忽略</returns>
    public bool Check(PathInfo path)
    {
        string full = path.GetFullPath();
        return full.EndsWith(".backup", StringComparison.OrdinalIgnoreCase)
            || Paths.Any(e => full.StartsWith(e.Trim('/','\\'), StringComparison.OrdinalIgnoreCase))
            || Extensions.Any(e=>full.EndsWith(e, StringComparison.OrdinalIgnoreCase))
            || Names.Any(e => full.EndsWith(e, StringComparison.OrdinalIgnoreCase))
            || Names.Any(e => e.Equals(path.GetName(),StringComparison.OrdinalIgnoreCase));
    }
    /// <summary>
    /// 检查是否被忽略
    /// </summary>
    /// <param name="path"></param>
    /// <returns>true 忽略</returns>
    public bool Check(string path) => Check((PathInfo)path);
}
