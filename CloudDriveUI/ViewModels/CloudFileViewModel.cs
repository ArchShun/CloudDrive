using CloudDriveUI.Models;
using CloudDriveUI.Utils;
using System.Threading.Tasks;
using System.Windows;

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
        SetFileItems();
    }

    protected async void SetFileItems()
    {
        var fileInfos = await cloudDrive.GetFileListAsync("/" + string.Join("/", Paths.Skip(1)));
        FileItems = new(fileInfos.Select(info =>
        {
            var file = new FileListItem()
            {
                Id = info.Id.ToString(),
                Name = info.Name,
                FileType = info.Category,
                IsDir = info.IsDir,
                Size = info.Size,
                RemoteUpdate = DateTimeUtils.TimeSpanToDateTime((long)info.ServerMtime)
            };
            return file;
        }));
    }

    protected override void OpenDir(string id)
    {
        var itm = fileItems.FirstOrDefault(e => e?.Id == id && e.IsDir, null);
        if (itm != null)
        {
            try
            {
                Paths.Add(itm.Name ?? "");
                SetFileItems();
            }
            catch (Exception ex)
            {
                Paths.RemoveAt(Paths.Count - 1);
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }


    protected override void NavDir(int? i)
    {
        if (i != null)
        {
            var end = (int)i + 1;
            Paths = new ObservableCollection<string>(Paths.ToArray()[..end]);
            SetFileItems();
        }
    }

}


