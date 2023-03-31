using Prism.Commands;

namespace CloudDriveUI.Models;

public class OperationItem
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public DelegateCommand<object?> Command { get; set; } = new DelegateCommand<object?>((obj) => { });
}
