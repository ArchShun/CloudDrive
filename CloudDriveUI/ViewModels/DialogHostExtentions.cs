using CloudDriveUI.Models;
using CloudDriveUI.Views;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;

namespace CloudDriveUI.ViewModels;

public static class DialogHostExtentions
{
    public static async Task<bool> ShowListDialogAsync(List<FormItem> items)
    {
        var copy = items.Select(e => new FormItem(e)).ToList();
        var flag = (bool)(await DialogHost.Show(new FormDialog(copy), "RootDialog") ?? false);
        if (flag)
            foreach (var i in items)
                i.Value = copy.Single(e => e.Key == i.Key).Value;
        return flag;
    }


   
}
