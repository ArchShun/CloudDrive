namespace CloudDriveUI.Models;

public class FormItem : BindableBase
{
    private string _value;

    public string Key { get; set; }

    public FormItem(FormItem item)
    {
        _value = item.Value;
        Key = item.Key;
        Value = item.Value;
        Validated = item.Validated;
        ValidatedMessage = item.ValidatedMessage;
    }

    public FormItem(string key, string value = "", Predicate<string>? validated = null, string validatedMessage = "")
    {
        Key = key;
        _value = value;
        if (validated != null) Validated = validated;
        ValidatedMessage = validatedMessage;
    }

    public string Value
    {
        get => _value; set
        {
            if (Validated.Invoke(value))
                SetProperty(ref _value, value);
            else throw new ArgumentException(ValidatedMessage);
        }
    }
    public Predicate<string> Validated { get; set; } = new Predicate<string>(str => true);
    public string ValidatedMessage { get; set; }
}
