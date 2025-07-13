using System;

namespace Pangolin.Desktop.Models;

[Serializable]
public class HttpHeader
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}