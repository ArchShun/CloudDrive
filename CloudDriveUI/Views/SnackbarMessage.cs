using CloudDriveUI.Core.Interfaces;
using MaterialDesignThemes.Wpf;

namespace CloudDriveUI.Views;

public class SnackbarMessage : SnackbarMessageQueue, ISnackbarMessage
{
    private SnackbarMessageQueue snackbar = new(TimeSpan.FromSeconds(1));

    public void Show(string msg)
    {
        Enqueue(msg);
    }

    public void Show(string msg, DateTime duration)
    {
        Enqueue(msg, null, null, duration);
    }
}
