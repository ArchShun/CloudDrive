using CloudDriveUI.PubSubEvents;
using Microsoft.Extensions.Configuration;
using Prism.Events;

namespace CloudDriveUI.Configurations;

public class AppConfiguration : BindableBase
{
    private static readonly string _path = "config.json";
    private static string _snapshot = "";
    private readonly IEventAggregator _aggregator;
    private SynchConfiguration synchFileConfig = new SynchConfiguration();
    private ThemeConfiguration appTheme = new();

    public AppConfiguration(IEventAggregator _aggregator)
    {
        this._aggregator = _aggregator;
        if (File.Exists(_path))
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddJsonFile("config.json", true, true);
            IConfigurationRoot root = builder.Build();
            SynchFileConfig = root.GetSection("SynchFileConfig").Get<SynchConfiguration>() ?? new();
            AppTheme = root.GetSection("AppTheme").Get<ThemeConfiguration>() ?? new();
        }
        _snapshot = JsonSerializer.Serialize(this);
    }

    public SynchConfiguration SynchFileConfig
    {
        get => synchFileConfig;
        set
        {
            synchFileConfig = value;
            RaisePropertyChanged();
        }
    }
    public ThemeConfiguration AppTheme
    {
        get => appTheme;

        set
        {
            appTheme = value;
            RaisePropertyChanged();
        }
    }
    public void Save()
    {
        if (HasChanged())
        {
            _snapshot = JsonSerializer.Serialize(this);
            File.WriteAllTextAsync(_path, _snapshot);
            _aggregator.GetEvent<AppConfigurationChangedEvent>().Publish();
        }
    }
    public bool HasChanged() => JsonSerializer.Serialize(this) != _snapshot;
}
