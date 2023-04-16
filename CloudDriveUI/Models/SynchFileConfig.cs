namespace CloudDriveUI.Models;

public record SynchFileConfig
{
    public string LocalPath { get; set; } = string.Empty;
    public string RemotePath { get; set; } = string.Empty;
    public int AutoRefreshSeconds { get; set; } = 60;
    public bool AutoRefresh { get; set; }=false;

}
