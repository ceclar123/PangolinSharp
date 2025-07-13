using System;
using System.Collections.Generic;
using ReactiveUI;

namespace Pangolin.Desktop.Models;

[Serializable]
public class HttpResponseDto : ReactiveObject
{
    public string? RawBody { get; set; }

    public string? PrettyBody { get; set; }
    public List<HttpCookie>? Cookies { get; set; }

    public List<HttpHeader>? Headers { get; set; }

    /// <summary>
    /// Status: 200 OK Times: 115 ms  Size:123 kb
    /// </summary>
    public string? Status { get; set; }

    public static HttpResponseDto Empty()
    {
        return new HttpResponseDto()
        {
            RawBody = string.Empty,
            PrettyBody = string.Empty,
            Cookies = new List<HttpCookie>(),
            Headers = new List<HttpHeader>(),
            Status = string.Empty
        };
    }
}