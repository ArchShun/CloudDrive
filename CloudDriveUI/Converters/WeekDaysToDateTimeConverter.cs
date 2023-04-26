using System.Globalization;
using System.Windows.Data;

namespace CloudDriveUI.Converters;

public class WeekDaysToDateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt) return dt.DayOfWeek;
        else return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        DayOfWeek? dw = null;
        if (value is DateTime dt) dw = dt.DayOfWeek;
        else if (value is DayOfWeek week) dw = week;
        if (dw != null)
        {
            DateTime today = DateTime.Today;
            int i = (int)(today.DayOfWeek - dw);
            return today.AddDays((7 - i) % 7);
        }
        else return value;
    }
}
