using CloudDriveUI.Models;
using CloudDriveUI.Utils;

namespace CloudDriveUI.ViewModels;

class CloudFileViewModel : FileViewBase
{

    /// <summary>
    /// 文件操作控件
    /// </summary>
    public List<OperationItem> OperationItems { get; } = new List<OperationItem>()
        {
            new OperationItem() { Name = "上传文件", Icon = "FileUploadOutline" },
            new OperationItem() { Name = "上传文件夹", Icon = "FolderUploadOutline" },
            new OperationItem() { Name = "新建文件夹", Icon = "FolderPlusOutline" }
        };



    public CloudFileViewModel(ICloudDriveProvider cloudDrive) : base(cloudDrive)
    {
        SetFileItemsAsync();
    }

    protected override async void SetFileItemsAsync()
    {
        var fileInfos = await cloudDrive.GetFileListAsync("/" + string.Join("/", Paths.Skip(1)));
        FileItems = new(fileInfos.Select(info =>
        {
            var file = new FileListItem() {
                Id = info.Id.ToString(),
                Name = info.Name,
                FileType = info.Category,
                IsDir = info.IsDir,
                Size = info.Size,
                RemoteUpdate = DateTimeUtils.TimeSpanToDateTime ((long)info.ServerMtime)
            };
            return file;
        }));
    }

}
