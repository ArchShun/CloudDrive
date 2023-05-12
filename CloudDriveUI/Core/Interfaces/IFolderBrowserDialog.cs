using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudDriveUI.Core.Interfaces;

public interface IFolderBrowserDialog
{
    public string SelectedPath { get; }
    public string[] SelectedPaths { get; }
    public bool? ShowDialog();
    public string Description { get; set; }
    public bool Multiselect { get; set; }
}
