using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CloudDriveUI.Converters;

class ObjectToStringEqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value?.ToString()?.Equals(parameter.ToString()) ?? false)
            return Visibility.Visible;
        else
            return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
