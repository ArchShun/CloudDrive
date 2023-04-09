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
    {SynchState.Added,"Plus" },
    { SynchState.Detached,"BlockHelper"},
    { SynchState.Consistent,"CheckboxMarkedCircleOutline"},
    { SynchState.Modified,"ArrowUpBold"},
    { SynchState.Conflict,"ExclamationThick"},
    {SynchState.ToUpdate ,"ArrowDownBold"} ,
    {SynchState.Deleted,"CloseThick" }
};

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var state = value as SynchState?;
        if (state != null && badgeIconDict.ContainsKey((SynchState)state)) return badgeIconDict[(SynchState)state];
        else return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
