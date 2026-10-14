using System;
using System.Globalization;

namespace Pangolin.SQLite.Models;

public sealed class RequestHistorySummary
{
    public long Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public string Method { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string DisplayTime => CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
}
