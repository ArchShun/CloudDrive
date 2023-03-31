using CloudDrive.Interfaces;

namespace CloudDrive;

public class CloudDriveBuilder
{
    private ICloudDriveSource? cloudDriveSource;

    public CloudDriveBuilder SetDriveSource(ICloudDriveSource cloudDriveSource)
    {
        this.cloudDriveSource = cloudDriveSource;
        return this;
    }
    public ICloudDrive? Build()
    {
        if (cloudDriveSource == null) return null;
        cloudDriveSource.Authorize();
        return cloudDriveSource.Build();
    }
    public static ICloudDrive Build(ICloudDriveSource cloudDriveSource)
    {
        cloudDriveSource.Authorize();
        return cloudDriveSource.Build();
    }
}
