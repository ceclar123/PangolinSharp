using System;
using System.Collections.Generic;
using Pangolin.Desktop.Models;
using ReactiveUI;

namespace Pangolin.Desktop.Models.Postman;

public enum HttpMethodType { GET, POST, PUT, DELETE, PATCH, OPTIONS, HEAD }
public enum RequestBodyMode { None, Raw, FormData, UrlEncoded }

public class FormDataField : KeyValueDto<string, string>
{
    private bool _isFile;
    public bool IsFile
    {
        get => _isFile;
        set => SetField(ref _isFile, value);
    }

    private string _historyFileName = string.Empty;
    public string HistoryFileName
    {
        get => _historyFileName;
        set => SetField(ref _historyFileName, value ?? string.Empty);
    }
}

// A snapshot of a request. Editing the UI while sending does not change this instance.
public class HttpRequestModel : ReactiveObject
{
    private HttpMethodType _method = HttpMethodType.GET;
    public HttpMethodType Method { get => _method; set => this.RaiseAndSetIfChanged(ref _method, value); }
    private string _url = string.Empty;
    public string Url { get => _url; set => this.RaiseAndSetIfChanged(ref _url, value ?? string.Empty); }
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<KeyValuePair<string, string>> HeaderEntries { get; set; } = new();
    public List<KeyValuePair<string, string>> QueryParameters { get; set; } = new();
    public List<FormDataField> FormFields { get; set; } = new();
    private string _body = string.Empty;
    public string Body { get => _body; set => this.RaiseAndSetIfChanged(ref _body, value ?? string.Empty); }
    public RequestBodyMode BodyMode { get; set; } = RequestBodyMode.Raw;
    public string BodyContentType { get; set; } = "application/json";
    public string AuthType { get; set; } = "No Auth";
    public string BearerToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiKeyName { get; set; } = string.Empty;
    public string ApiKeyValue { get; set; } = string.Empty;
    public bool ApiKeyInQuery { get; set; }
    public bool FollowRedirects { get; set; } = true;
    public bool VerifySslCertificate { get; set; } = true;
    public bool AutomaticallyEncodeUrl { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}

public class HttpResponseModel
{
    public int StatusCode { get; set; }
    public string ReasonPhrase { get; set; } = string.Empty;
    public Dictionary<string, IEnumerable<string>> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string Body { get; set; } = string.Empty;
    public byte[] BodyBytes { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public bool IsBinary { get; set; }
    public string FinalUrl { get; set; } = string.Empty;
    public string HttpVersion { get; set; } = string.Empty;
    public long ElapsedMilliseconds { get; set; }
}

public class ResponseCookie
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Expires { get; init; } = string.Empty;
    public string SameSite { get; init; } = string.Empty;
    public bool Secure { get; init; }
    public bool HttpOnly { get; init; }

    public static ResponseCookie Parse(string header)
    {
        var parts = header.Split(';');
        var pair = parts[0].Split('=', 2);
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts[1..])
        {
            var attribute = part.Trim().Split('=', 2);
            attributes[attribute[0]] = attribute.Length > 1 ? attribute[1] : string.Empty;
        }
        return new ResponseCookie
        {
            Name = pair[0].Trim(), Value = pair.Length > 1 ? pair[1].Trim() : string.Empty,
            Domain = attributes.GetValueOrDefault("Domain", string.Empty),
            Path = attributes.GetValueOrDefault("Path", string.Empty),
            Expires = attributes.GetValueOrDefault("Expires", attributes.TryGetValue("Max-Age", out var age) ? $"Max-Age={age}" : string.Empty),
            SameSite = attributes.GetValueOrDefault("SameSite", string.Empty),
            Secure = attributes.ContainsKey("Secure"), HttpOnly = attributes.ContainsKey("HttpOnly")
        };
    }
}
