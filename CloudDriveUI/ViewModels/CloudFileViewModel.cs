using CloudDriveUI.Models;
using System.Windows;

namespace CloudDriveUI.ViewModels;

class CloudFileViewModel : FileViewBase
{

    protected ObservableCollection<CloudFileItem> fileItems = new();


    public CloudFileViewModel(ICloudDriveProvider cloudDrive) : base(cloudDrive)
    {
        SetFileItems();
    }

    /// <summary>
    /// 文件操作控件
    /// </summary>
    public List<OperationItem> OperationItems { get; } = new List<OperationItem>()
    {
        new OperationItem() { Name = "上传文件", Icon = "FileUploadOutline" },
        new OperationItem() { Name = "上传文件夹", Icon = "FolderUploadOutline" },
        new OperationItem() { Name = "新建文件夹", Icon = "FolderPlusOutline" }
    };

    /// <summary>
    /// 需要显示的文件列表
    /// </summary>
    public ObservableCollection<CloudFileItem> FileItems { get => fileItems; set => SetProperty(ref fileItems, value); }
  
    
    protected async void SetFileItems()
    {
        var res = await cloudDrive.GetFileListAsync("/" + string.Join("/", Paths.Skip(1)));
        var items = res.Select(e => new CloudFileItem(e)).ToArray();
        FileItems = new ObservableCollection<CloudFileItem>(items);
    }

    protected override void OpenDirAsync(FileItemBase itm)
    {
        if (itm.IsDir)
        {
            try
            {
                Paths.Add(itm.Name);
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


