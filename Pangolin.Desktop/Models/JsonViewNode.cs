using System;
using System.Collections.ObjectModel;

namespace Pangolin.Desktop.Models;

[Serializable]
public class JsonViewNode
{
    public ObservableCollection<JsonViewNode>? SubNodes { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? ShowValue => Name + Value;
}