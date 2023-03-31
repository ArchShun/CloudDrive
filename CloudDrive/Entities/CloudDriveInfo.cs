namespace CloudDrive.Entities;

public record CloudDriveInfo
{
    public int Totle { get; set; }
    public int Used { get; set; }
    public int Free { get; set; }
}
