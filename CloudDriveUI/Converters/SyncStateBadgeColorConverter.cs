using CloudDriveUI.Models;
using System.Globalization;
using System.Windows.Data;

namespace CloudDriveUI.Converters;

class SyncStateBadgeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var state = (SynchState)value;
        var color = state == SynchState.Detached ? "translate" : (state == SynchState.Consistent ? "green" : "red");
        return color;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
