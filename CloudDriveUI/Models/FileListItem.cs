namespace CloudDriveUI.Models;

/// <summary>
/// 文件列表显示项目
/// </summary>
public record FileListItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;

    public DateTime? RemoteUpdate { get; set; }
    public DateTime LocalUpdate { get; set; } = DateTime.Now;
    public long Size { get; set; }
    public FileType? FileType { get; set; } 
    public bool IsDir { get; set; } = false;

    public SynchState State { get; set; } = SynchState.Detached;

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}