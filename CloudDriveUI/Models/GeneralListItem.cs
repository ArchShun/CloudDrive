using Prism.Commands;

namespace CloudDriveUI.Models;

public class GeneralListItem
{
    public string? Name { get; set; }
    public string? Info { get; set; }
    public string? Icon { get; set; }
    public DelegateCommand<object?> Command { get; set; } = new DelegateCommand<object?>((obj) => { });
}
