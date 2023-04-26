using CloudDriveUI.Models;
using CloudDriveUI.PubSubEvents;
using Prism.Events;
using System.Runtime.CompilerServices;

namespace CloudDriveUI.Configurations;

public class SynchConfiguration : BindableBase
{
    private string localPath = string.Empty;

    public string LocalPath
    {
        get => localPath; set
        {
            localPath = value;
            RaisePropertyChanged();
        }
    }
    public string RemotePath { get; set; } = string.Empty;
    public int AutoRefreshSeconds { get; set; } = 60;
    public bool AutoRefresh { get; set; } = true;
    public SynchIgnore Ignore { get; set; } = new();
    public bool UseSchedule { get; set; } = true;
    public SynchFrequency Frequency { get; set; } = SynchFrequency.Daily;
    public DateTime Schedule { get; set; } = DateTime.Now;
}
