
namespace CloudDriveUI.Models;

public record AppConfig
{
    private readonly string _path = "config.json";

    public SynchFileConfig SynchFileConfig { get; set; } = new SynchFileConfig();

    public void SaveAsync()
    {
        File.WriteAllTextAsync(_path, JsonSerializer.Serialize(this));
    }
}
