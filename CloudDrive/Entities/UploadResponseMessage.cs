namespace CloudDrive.Entities;

public record UploadResponseMessage : ResponseMessage
{
    public CloudFileInfo? Content { get; set; }

    public UploadResponseMessage(bool isSuccess, CloudFileInfo? content = null, string errMessage = "") : base(isSuccess, errMessage)
    {
        Content = content;
    }
}
