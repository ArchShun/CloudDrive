namespace CloudDriveUI.Utils;

public static class DateTimeUtils
{


    /// <summary>
    /// 本地时间转换为时间戳
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static long GetTimeSpan(DateTime time)
    {
        DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
        return (long)(time - startTime).TotalSeconds;
    }


    /// <summary>
    /// 时间戳转换为本地时间
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public static DateTime TimeSpanToDateTime(long span)
    {
        DateTime time = DateTime.MinValue;
        DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
        time = startTime.AddSeconds(span);
        return time;
    }
}
