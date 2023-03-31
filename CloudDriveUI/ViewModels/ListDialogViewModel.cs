namespace CloudDriveUI.ViewModels;

public class ListDialogViewModel : BindableBase
{
    public ObservableCollection<KeyValueItem<string, string?>> ListItems { get; set; } = new() { };
}
public record KeyValueItem<KT, VT>
{
    public KT Key { get; set; }

    public KeyValueItem(KT key, VT? value)
    {
        Key = key;
        Value = value;
    }


    public VT? Value { get; set; }

}
