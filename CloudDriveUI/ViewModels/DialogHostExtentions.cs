using CloudDriveUI.Models;
using CloudDriveUI.Views;
using ImTools;
using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;

namespace CloudDriveUI.ViewModels;

public static class DialogHostExtentions
{

    public static Task ShowMessageDialog(string message)
    {
        var dialog = new MessageDialog();
        dialog.DataContext = message;
        return DialogHost.Show(dialog, "RootDialog");
    }
    public static async Task<bool> ShowListDialogAsync(List<FormItem> items)
    {
        var copy = items.Select(e => new FormItem(e)).ToList();
        var flag = (bool)(await DialogHost.Show(new FormDialog(copy), "RootDialog") ?? false);
        if (flag)
            foreach (var i in items)
                i.Value = copy.FindFirst(e => e.Key == i.Key).Value;
        return flag;
    }


    private static readonly CircleProgressBar circleProgressBar = new();
    public static void ShowCircleProgressBar()
    {
        DialogHost.Show(circleProgressBar, "ProgressBar");
    }

    public static void CloseCircleProgressBar()
    {
        try
        {
            if (DialogHost.IsDialogOpen("ProgressBar"))
                DialogHost.Close("ProgressBar");
        }
        catch
        {

        }
    }

    public static void SelectFileDialog()
    {
        throw new NotImplementedException();
    }
}
