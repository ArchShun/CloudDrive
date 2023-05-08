namespace CloudDriveUI.Domain.Entities;

public abstract class FileItemBase : BindableBase
{
    private static readonly Dictionary<FileType, string> icons = new()
    {
        {FileType.Video,"VideoOutline" },
        {FileType.Audio,"Music"},
        {FileType.Picture,"FileImageOutline"},
        {FileType.Document,"FileDocumentOutline"},
        {FileType.Application,"ApplicationCogOutline"},
        {FileType.Unknown,"FileQuestionOutline"},
        {FileType.BitTorrent,"DownloadLockOutline"}
    };

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract bool IsDir { get; }
    public abstract string Size { get; }
    public abstract FileType FileType { get; }
    public string Icon { get => IsDir ? "FolderOutline" : icons[FileType]; }

}