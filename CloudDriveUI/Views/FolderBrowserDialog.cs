using CloudDriveUI.Core.Interfaces;
using Ookii.Dialogs.Wpf;

namespace CloudDriveUI.Views;

public class FolderBrowserDialog : IFolderBrowserDialog
{
    private readonly VistaFolderBrowserDialog _dialog = new();

    public string SelectedPath => _dialog.SelectedPath;
    public string Description { get => _dialog.Description; set => _dialog.Description = value; }
    public bool Multiselect { get => _dialog.Multiselect; set => _dialog.Multiselect = value; }

    public string[] SelectedPaths => _dialog.SelectedPaths;

    public bool? ShowDialog()
    {
        return _dialog.ShowDialog();
    }
}
