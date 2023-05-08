namespace CloudDriveUI.PubSubEvents;

internal class NavigateRequestEventArgs
{
    public string Name { get; set; }
    public List<KeyValuePair<string, object>> Params { get; set; } = new List<KeyValuePair<string, object>>();
    public NavigateRequestEventArgs(string name)
    {
        Name = name;
    }

    public NavigateRequestEventArgs(string name, params KeyValuePair<string, object>[] pairs) : this(name)
    {
        Params = pairs.ToList();
    }
}
