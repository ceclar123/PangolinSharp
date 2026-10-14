namespace Pangolin.SQLite.Models;

public sealed class RequestHistorySnapshot
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public List<RequestHistoryHeader> Headers { get; set; } = new();
    public List<RequestHistoryParameter> QueryParameters { get; set; } = new();
    public string BodyMode { get; set; } = "none";
    public string BodyContentType { get; set; } = "application/json";
    public string BodyText { get; set; } = string.Empty;
    public List<RequestHistoryFormField> FormFields { get; set; } = new();
    public string AuthType { get; set; } = "No Auth";
    public RequestHistoryAuthConfiguration Auth { get; set; } = new();
    public RequestHistorySettings Settings { get; set; } = new();
    public bool SecretsRedacted { get; set; }
}

public sealed class RequestHistoryHeader
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool WasRedacted { get; set; }
}

public sealed class RequestHistoryParameter
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool WasRedacted { get; set; }
}

public sealed class RequestHistoryFormField
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsFile { get; set; }
    public bool WasRedacted { get; set; }
}

public sealed class RequestHistoryAuthConfiguration
{
    public string BearerToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiKeyName { get; set; } = string.Empty;
    public string ApiKeyValue { get; set; } = string.Empty;
    public int ApiKeyLocation { get; set; }
}

public sealed class RequestHistorySettings
{
    public bool FollowRedirects { get; set; } = true;
    public bool VerifySslCertificate { get; set; } = true;
    public bool AutomaticallyEncodeUrl { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}
