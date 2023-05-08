using CloudDriveUI.Models;

namespace CloudDriveUI.Configurations;

public class SynchConfiguration : BindableBase
{
    private string localPath = "";
    public string LocalPath
    {
        get => localPath; set
        {
            localPath = Path.GetFullPath(value);
            RaisePropertyChanged();
        }
    }
    public string RemotePath { get; set; } = "";
    public int AutoRefreshSeconds { get; set; } = 300;
    public bool AutoRefresh { get; set; } = true;
    public SynchIgnore Ignore { get; set; } = new();
    public bool UseSchedule { get; set; } = true;
    public SynchFrequency Frequency { get; set; } = SynchFrequency.Daily;
    public DateTime Schedule { get; set; } = DateTime.Now;
}
