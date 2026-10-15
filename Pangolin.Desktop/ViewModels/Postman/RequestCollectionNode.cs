using System.Collections.ObjectModel;
using Pangolin.SQLite.Models;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Postman;

public sealed class RequestCollectionNode : ReactiveObject
{
    public RequestCollectionNode(RequestCollection collection, int depth, string path)
    {
        Id = collection.Id;
        Name = collection.Name;
        ParentId = collection.ParentId;
        Depth = depth;
        RequestCount = collection.RequestCount;
        Path = path;
    }

    public long Id { get; }
    public string Name { get; }
    public long? ParentId { get; }
    public int Depth { get; }
    public int RequestCount { get; }
    public string Path { get; }
    public RequestCollectionNode? Parent { get; internal set; }
    public ObservableCollection<RequestCollectionNode> Children { get; } = new();
    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }
}
