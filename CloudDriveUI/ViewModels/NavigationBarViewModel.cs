using CloudDriveUI.Models;
using CloudDriveUI.PubSubEvents;
using ImTools;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Collections;
using System.Configuration.Install;
using System.Windows.Forms;

namespace CloudDriveUI.ViewModels;

public class NavigationBarViewModel : BindableBase
{
    private readonly IRegionManager regionManager;
    private int selectedIndex;

    public NavigationBarViewModel(IRegionManager regionManager, IEventAggregator aggregator)
    {
        this.regionManager = regionManager;
        NavigateCommand = new(Navigate);
        _ = aggregator.GetEvent<NavigateRequestEvent>().Subscribe(str =>
        {
            var i = Items.FindIndex(e => e.Name == str);
            if (i > -1)
            {
                Navigate(Items[i]);
                SelectedIndex=i;
            }
        });

        Items.Add(new GeneralListItem() { Name = "CloudFileView", Info = "文件" });
        Items.Add(new GeneralListItem() { Name = "SynchFileView", Info = "同步空间" });
        Items.Add(new GeneralListItem() { Name = "PreferencesView", Info = "配置中心" });

        // 初始化界面
        Navigate(Items[0]);
        SelectedIndex = 0;
    }
    public List<GeneralListItem> Items { get; private set; } = new();
    public DelegateCommand<object?> NavigateCommand { get; }
    public int SelectedIndex { get => selectedIndex; set { selectedIndex = value; RaisePropertyChanged(); } }

    void Navigate(object? obj)
    {
        if (obj is GeneralListItem itm)
        {
            NavigationParameters keys = new()
            {
                { "title", itm.Info }
            };
            regionManager.Regions["ContentRegion"].RequestNavigate(itm.Name, keys);
        }
    }

}
