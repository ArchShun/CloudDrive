using System.Collections.Generic;

namespace CloudDrive.Entities;

public record CloudFileInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PathInfo Path { get; set; } = new PathInfo();
    public FileType? Category { get; set; }
    public bool IsDir { get; set; } = false;
    public long Size { get; set; } = 0;
    public long ServerCtime { get; set; } = 0;
    public long ServerMtime { get; set; } = 0;

    public long? LocalCtime { get; set; }
    public long? LocalMtime { get; set; }
    public Dictionary<string, object?> XData { get; set; } = new();
}
