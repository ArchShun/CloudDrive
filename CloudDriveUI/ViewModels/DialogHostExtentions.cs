using CloudDriveUI.Views;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CloudDriveUI.ViewModels;

public static class DialogHostExtentions
{
    /// <summary>
    /// 根据字典创建表单
    /// </summary>
    /// <param name="keyValuePairs">字典</param>
    /// <returns>是否提交</returns>
    public static async Task<bool> ShowListDialogAsync(Dictionary<string, string?> keyValuePairs)
    {
        var dialog = new ListDialog();
        var vm = dialog.DataContext as ListDialogViewModel;
        if (vm == null)
        {
            vm = new ListDialogViewModel();
            dialog.DataContext = vm;
        }
        vm.ListItems.AddRange(keyValuePairs.Select(kv => new KeyValueItem<string, string?>(kv.Key, kv.Value)));

        var flag = (bool)(await DialogHost.Show(dialog, "RootDialog") ?? false);

        if (flag)
        {
            foreach (var kv in vm.ListItems)
            {
                if (keyValuePairs.ContainsKey(kv.Key))
                {
                    keyValuePairs[kv.Key] = kv.Value;
                }
            }
        }
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



}
