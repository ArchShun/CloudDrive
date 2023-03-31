namespace CloudDriveUI.Models;

public record SynchFileConfig
{
    public string LocalPath { get; set; } = string.Empty;
    public string RemotePath { get; set; } = string.Empty;
}
