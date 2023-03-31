namespace CloudDrive.Interfaces;

public interface ICloudDriveSource
{
    public bool Authorize();

    public ICloudDrive Build();
}
