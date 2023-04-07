using CloudDrive.Utils;
using CloudDriveUI.Models;
using CloudDriveUI.Utils;
using ImTools;
using MaterialDesignThemes.Wpf;
using System.Globalization;
using System.Windows.Data;

namespace CloudDriveUI.Converters;

public class FileListItemsConverter : IValueConverter
{

    private readonly Dictionary<FileType, string> icons = new()
        {
            {FileType.Video,"VideoOutline" },
            {FileType.Audio,"Music"},
            {FileType.Picture,"FileImageOutline"},
            {FileType.Document,"FileDocumentOutline"},
            {FileType.Application,"ApplicationCogOutline"},
            {FileType.Other,"FileQuestionOutline"},
            {FileType.BitTorrent,"DownloadLockOutline"}
        };

    private readonly Dictionary<SynchState, string> badgeIconDict = new() {
        {SynchState.Added,"Plus" },
        { SynchState.Detached,"BlockHelper"},
        { SynchState.Consistent,"CheckboxMarkedCircleOutline"},
        { SynchState.Modified,"ArrowUpBold"},
        { SynchState.Conflict,"ExclamationThick"},
        {SynchState.ToUpdate ,"ArrowDownBold"} ,
        {SynchState.Deleted,"CloseThick" }
    };

    /// <summary>
    /// 获取文件列表图标
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private string GetIcon(FileType? type)
    {
        if (type == null) return "";

        return icons[(FileType)type];
    }



    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var param = parameter as string;
        IEnumerable<FileListItem> values = (IEnumerable<FileListItem>)value;
        switch (param)
        {
            case "CloudFileView":
                return CloudFileViewConvert(values);
            case "SynchFileView":
                return SynchFileViewConvert(values);
            default:
                return new { };
        }
    }
    /// <summary>
    /// 云文件视图转换
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    private object CloudFileViewConvert(IEnumerable<FileListItem> values)
    {
        return values.Select<FileListItem, object>(file => new
        {
            file.Id,
            Name = file.Name ?? "",
            Icon = file.IsDir ? "FolderOutline" : GetIcon(file.FileType),
            Update = file.RemoteUpdate == null ? "" : ((DateTime)file.RemoteUpdate).ToString("yyyy-MM-dd HH:mm"),
            Size = file.Size > 0 ? FileUtils.CalSize(file.Size) : "--"
        });
    }

    /// <summary>
    /// 同步空间转换
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    private object SynchFileViewConvert(IEnumerable<FileListItem> values)
    {
        return values.OrderBy(e => !e.IsDir).Select<FileListItem, object>(file => new
        {
            file.Id,
            Name = file.Name ?? "",
            Icon = file.IsDir ? "FolderOutline" : GetIcon(file.FileType),
            RemoteUpdate = file.RemoteUpdate == null ? "" : ((DateTime)file.RemoteUpdate).ToString("yyyy-MM-dd HH:mm"),
            LocalUpdate = ((DateTime)file.LocalUpdate).ToString("yyyy-MM-dd HH:mm"),
            Size = FileUtils.CalSize(file.Size),
            BadgeIcon = badgeIconDict[file.State],
            BadgeColor = file.State == SynchState.Detached ? "translate" : (file.State == SynchState.Consistent ? "green" : "red")
            //Opacity = file.State == SynchState. ? "translate" : (file.State == SynchState.Consistent ? "green" : "red")
        });
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


