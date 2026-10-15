namespace Pangolin.SQLite.Models;

public sealed class RequestCollection
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public long? ParentId { get; init; }
    public int RequestCount { get; init; }
}
