using System.Globalization;
using System.Windows.Data;

namespace CloudDriveUI.Converters;

public class StringToListConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<object> lst)
        {
            var sepr = parameter.ToString() == "$" ? Environment.NewLine : parameter.ToString();
            return string.Join(sepr, lst);
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var sepr = parameter.ToString() == "$" ? Environment.NewLine : parameter.ToString();
        return value?.ToString()?.Split(sepr).Where(e => !string.IsNullOrEmpty(e)).ToList() ?? new List<string>();
    }
}
