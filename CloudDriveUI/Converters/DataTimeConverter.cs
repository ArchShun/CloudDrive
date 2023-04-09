using System.Globalization;
using System.Windows.Data;

namespace CloudDriveUI.Converters;

class DataTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return "";
        else
        {
            var dt = (DateTime)value;
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
