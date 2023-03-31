using Prism.Commands;
using Prism.Regions;

namespace CloudDriveUI.ViewModels;

public class NavigationBarViewModel : BindableBase
{
    private readonly IRegionManager regionManager;
    public NavigationBarViewModel(ICloudDrive cloudDrive, IRegionManager regionManager)
    {
        this.cloudDrive = cloudDrive;
        SelectedIndex = 0;
        SelectedItem = ItemsList[0];
        this.regionManager = regionManager;

        OpenCommand = new DelegateCommand(Open);
        Open();

    }

    private readonly ICloudDrive cloudDrive;

    public DelegateCommand OpenCommand { get; }
    public List<string> ItemsList { get; } = new List<string>() { "文件", "同步空间" };//,"相册", "收藏夹", "密码箱", "传输列表" };
    public List<string> NavPageName { get; } = new List<string>() { "CloudFileView", "SynchFileView" };//, "File", "File", "File" };
    public int SelectedIndex { get; set; }
    public object SelectedItem { get; set; }


    void Open()
    {
        int i = SelectedIndex;
        NavigationParameters keys = new NavigationParameters();
        keys.Add("title", ItemsList[i]);
        regionManager.Regions["ContentRegion"].RequestNavigate(NavPageName[i], keys);
    }

}
