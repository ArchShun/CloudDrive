namespace CloudDriveUI.Core.Interfaces;

public interface ISelectFileDialog
{
    public string Title { get; set; }
    public bool Multiselect { get; set; }
    public string FileName { get; }
    public string[] FileNames { get; }
    public bool? ShowDialog();
}
