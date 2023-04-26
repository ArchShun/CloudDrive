using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;

namespace CloudDriveUI.Configurations;

public class AppConfiguration : BindableBase
{
    private static readonly string _path = "config.json";
    private static string _snapshot = "";
    private static AppConfiguration? _instance = null;
    private SynchConfiguration synchFileConfig = new SynchConfiguration();
    private ThemeConfiguration appTheme = new();

    public AppConfiguration() { }
    public SynchConfiguration SynchFileConfig
    {
        get => synchFileConfig; set
        {
            synchFileConfig = value;
            RaisePropertyChanged();
        }
    }

    public ThemeConfiguration AppTheme
    {
        get => appTheme; set
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
        }
    }
    public bool HasChanged() => JsonSerializer.Serialize(this) != _snapshot;

    public static AppConfiguration Instance { private set; get; } = _instance ?? Load();

    private static AppConfiguration Load()
    {
        FileInfo file = new(_path);
        AppConfiguration? tmp = null;
        if (file.Exists)
        {
            using var stream = file.OpenRead();
            tmp = JsonSerializer.Deserialize<AppConfiguration>(stream);
        }
        var ret = tmp ?? new AppConfiguration();
        _snapshot = JsonSerializer.Serialize(ret);
        return ret;
    }
}
