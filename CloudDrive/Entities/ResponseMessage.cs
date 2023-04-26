namespace CloudDrive.Entities;

public record ResponseMessage
{
    public bool IsSuccess { get; set; } = true;

    public ResponseMessage() { }

    public ResponseMessage(bool isSuccess, string errMessage = "")
    {
        IsSuccess = isSuccess;
        ErrMessage = errMessage;
    }

    public string ErrMessage { get; set; } = string.Empty;
}
