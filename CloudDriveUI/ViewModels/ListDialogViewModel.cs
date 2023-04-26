using Prism.Commands;
using System.ComponentModel.DataAnnotations;

namespace CloudDriveUI.ViewModels;

public class ListDialogViewModel : BindableBase
{
    public ObservableCollection<ListDialogItem> ListItems { get; set; } = new() { };
}
public record ListDialogItem(string Key, string? Value = null);
