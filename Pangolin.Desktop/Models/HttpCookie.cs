using System;

namespace Pangolin.Desktop.Models;

[Serializable]
public class HttpCookie
{
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? Domain { get; set; }
    public string? Path { get; set; }
    public DateTime? Expires { get; set; }
    public bool? Expired { get; set; }
    public bool? HttpOnly { get; set; }
    public bool? Secure { get; set; }
}