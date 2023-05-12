namespace CloudDriveUI.Core.Interfaces;

public interface ISnackbarMessage
{
    public void Show(string msg);
    public void Show(string msg, DateTime duration);
}
