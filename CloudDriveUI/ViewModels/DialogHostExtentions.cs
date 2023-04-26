using CloudDriveUI.Views;
using ImTools;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CloudDriveUI.ViewModels;

public static class DialogHostExtentions
{

    public static Task ShowMessageDialog(string message)
    {
        var dialog = new MessageDialog();
        dialog.DataContext = message;
        return DialogHost.Show(dialog, "RootDialog");
    }

    /// <summary>
    /// 根据字典创建表单
    /// </summary>
    /// <param name="keyValuePairs">字典</param>
    /// <returns>是否提交</returns>
    public static async Task<bool> ShowListDialogAsync(Dictionary<string, string?> keyValuePairs, Func<ListDialogItem, ValidationResult>? validation = null)
    {
        var dialog = new ListDialog();
        var vm = new ListDialogViewModel();
        dialog.DataContext = vm;
        vm.ListItems.AddRange(keyValuePairs.Select(e => new ListDialogItem(e.Key, e.Value)));
        var flag = (bool)(await DialogHost.Show(dialog, "RootDialog") ?? false);
        if (flag) vm.ListItems.Where(e => keyValuePairs.ContainsKey(e.Key)).ToList().ForEach(kv => keyValuePairs[kv.Key] = kv.Value);
        return flag;
    }

    /// <summary>
    /// 根据键名列表创建表单
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="pairs">字典</param>
    /// <returns>是否提交</returns>
    public static Task<bool> ShowListDialogAsync(IEnumerable<string> keys, out Dictionary<string, string?> pairs)
    {
        pairs = new Dictionary<string, string?>(keys.Select(k => new KeyValuePair<string, string?>(k, null)));
        return ShowListDialogAsync(pairs);
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
