using BDCloudDrive.Utils;
using CloudDrive.Utils;

namespace CloudDriveUI.Models;

class CloudFileItem : FileItemBase
{
    private readonly CloudFileInfo cloudFileInfo;

    public CloudFileItem(CloudFileInfo cloudFileInfo)
    {
        this.cloudFileInfo = cloudFileInfo;
    }

    #region 属性
    public override string Id => cloudFileInfo.Id.ToString();
    public override string Name => cloudFileInfo.Name;
    public override bool IsDir => cloudFileInfo.IsDir;
    public override FileType FileType => cloudFileInfo.Category ?? FileType.Other;
    public override string Size => IsDir ? "--" : FileUtils.CalSize(cloudFileInfo.Size);
    public DateTime Update => DateTimeUtils.TimeSpanToDateTime(cloudFileInfo.ServerMtime);

    #endregion


}
