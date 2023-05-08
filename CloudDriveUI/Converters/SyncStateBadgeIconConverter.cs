using CloudDriveUI.Models;
using System.Globalization;
using System.Windows.Data;

namespace CloudDriveUI.Converters;

public class SyncStateBadgeIconConverter : IValueConverter
{
    /// <summary>
    /// 状态图标
    /// </summary>
    private static readonly Dictionary<SynchState, string> badgeIconDict = new() {
    { SynchState.Added,"Plus" },
    { SynchState.Modified,"ArrowUpBold"},
    { SynchState.RemoteAdded ,"CloudPlusOutline"} ,
    { SynchState.RemoteModified ,"ArrowDownBold"} ,
    { SynchState.Conflict,"ExclamationThick"},
    { SynchState.Deleted,"CloseThick" },
    { SynchState.Unknown,"Help" },
    { SynchState.Consistent,"CheckboxMarkedCircleOutline"},
    { SynchState.Detached,"BlockHelper"},
};

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SynchState state)
        {
            foreach (var k in badgeIconDict.Keys)
                if (state.HasFlag(k)) return badgeIconDict[k];
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
