using CloudDriveUI.Core.Interfaces;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudDriveUI.Views;

internal class SelectFileDialog : ISelectFileDialog
{
    VistaOpenFileDialog dialog = new VistaOpenFileDialog();
    public string Title { get => dialog.Title; set => dialog.Title = value; }
    public bool Multiselect { get => dialog.Multiselect; set => dialog.Multiselect = value; }

    public string FileName => dialog.FileName;

    public string[] FileNames => dialog.FileNames;

    public bool? ShowDialog()
    {
        return dialog.ShowDialog();
    }
}
