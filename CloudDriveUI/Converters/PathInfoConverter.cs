using System.Globalization;
using System.Windows.Data;

namespace CloudDriveUI.Converters;

public class PathInfoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PathInfo info)
        {
            return info.GetSegmentPath();
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
