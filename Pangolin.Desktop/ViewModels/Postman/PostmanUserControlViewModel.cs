using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pangolin.Desktop.Models;
using Pangolin.Desktop.Models.Postman;
using Pangolin.Desktop.Services;
using ReactiveUI;
using Command = ReactiveUI.ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid>;

namespace Pangolin.Desktop.ViewModels.Postman;

public class PostmanUserControlViewModel : ViewModelBase
{
    private readonly HttpClientService _service = new();
    private CancellationTokenSource? _requestCancellation;
    private CancellationTokenSource? _formatCancellation;
    private int _formatVersion;
    public Task ResponseFormattingTask { get; private set; } = Task.CompletedTask;
    private bool _isFormatting;
    public bool IsFormatting
    {
        get => _isFormatting;
        private set => this.RaiseAndSetIfChanged(ref _isFormatting, value);
    }
    private string _rawResponseBody = string.Empty;
    private byte[] _responseBytes = Array.Empty<byte>();
    private bool _isBinaryResponse;
    public string RawResponseBody => _rawResponseBody;
    public byte[] ResponseBytes => _responseBytes;
    public string ResponseContentType { get; private set; } = string.Empty;
    public bool HasResponseMessage => !string.IsNullOrEmpty(ResponseMessage);
    public bool ShowResponsePlaceholder => !HasResponse && !IsSending && !HasResponseMessage;
    public bool IsImagePreview => HasResponse && ResponseContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        && ResponseBodyMode == 0 && ResponseFormat == "Binary";

    public PostmanUserControlViewModel()
    {
        QueryParams.Add(new());
        Headers.Add(new());
        FormDataParams.Add(new());
        var canSend = this.WhenAnyValue(x => x.IsSending).Select(sending => !sending);
        CmdSend = ReactiveCommand.CreateFromTask(SendAsync, canSend);
        CmdCancel = ReactiveCommand.Create(CancelRequest, this.WhenAnyValue(x => x.IsSending));
        CmdClearResponse = ReactiveCommand.Create(ClearResponse, canSend);
        CmdAddQueryParam = ReactiveCommand.Create(() => QueryParams.Add(new()));
        CmdAddHeader = ReactiveCommand.Create(() => Headers.Add(new()));
        CmdAddFormDataParam = ReactiveCommand.Create(() => FormDataParams.Add(new()));
        CmdImportQuery = ReactiveCommand.Create(ImportQuery);
        CmdToggleHeaderBulkEdit = ReactiveCommand.Create(ToggleHeaderBulkEdit);
    }

    public HttpRequestModel Request { get; } = new();
    public ObservableCollection<KeyValueDto<string, string>> Headers { get; } = new();
    public ObservableCollection<KeyValueDto<string, string>> QueryParams { get; } = new();
    public ObservableCollection<FormDataField> FormDataParams { get; } = new();
    public ObservableCollection<KeyValueDto<string, string>> ResponseHeaders { get; } = new();
    public ObservableCollection<ResponseCookie> ResponseCookies { get; } = new();
    public HttpMethodType[] Methods { get; } = Enum.GetValues<HttpMethodType>();
    public string[] MediaTypes { get; } = { "JSON", "Text", "XML", "HTML" };
    public string[] AuthTypes { get; } = { "No Auth", "API Key", "Bearer Token", "Basic Auth" };
    public string[] BodyModes { get; } = { "none", "raw", "form-data", "x-www-form-urlencoded" };
    public string[] ApiKeyLocations { get; } = { "Header", "Query Params" };
    public string[] ResponseFormats { get; } = { "JSON", "XML", "HTML", "Text", "Binary" };

    private string _selectedMediaType = "JSON";
    public string SelectedMediaType
    {
        get => _selectedMediaType;
        set => this.RaiseAndSetIfChanged(ref _selectedMediaType, value ?? string.Empty);
    }
    private string _selectedAuthType = "No Auth";
    public string SelectedAuthType
    {
        get => _selectedAuthType;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedAuthType, value ?? string.Empty);
            this.RaisePropertyChanged(nameof(IsBearerAuth));
            this.RaisePropertyChanged(nameof(IsBasicAuth));
            this.RaisePropertyChanged(nameof(IsApiKeyAuth));
            this.RaisePropertyChanged(nameof(IsNoAuth));
        }
    }
    public bool IsBearerAuth => SelectedAuthType == "Bearer Token";
    public bool IsBasicAuth => SelectedAuthType == "Basic Auth";
    public bool IsApiKeyAuth => SelectedAuthType == "API Key";
    public bool IsNoAuth => SelectedAuthType == "No Auth";
    private int _selectedBodyMode;
    public int SelectedBodyMode
    {
        get => _selectedBodyMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedBodyMode, value);
            this.RaisePropertyChanged(nameof(IsRawBody));
            this.RaisePropertyChanged(nameof(IsFormBody));
            this.RaisePropertyChanged(nameof(IsMultipartBody));
            this.RaisePropertyChanged(nameof(IsNoBody));
        }
    }
    public bool IsRawBody => SelectedBodyMode == 1;
    public bool IsFormBody => SelectedBodyMode is 2 or 3;
    public bool IsMultipartBody => SelectedBodyMode == 2;
    public bool IsNoBody => SelectedBodyMode == 0;

    private string _responseFormat = "JSON";
    public string ResponseFormat
    {
        get => _responseFormat;
        set { this.RaiseAndSetIfChanged(ref _responseFormat, value ?? string.Empty); ResponseFormattingTask = UpdateResponseBodyAsync(); }
    }
    private int _responseBodyMode;
    public int ResponseBodyMode
    {
        get => _responseBodyMode;
        set { this.RaiseAndSetIfChanged(ref _responseBodyMode, value); ResponseFormattingTask = UpdateResponseBodyAsync(); }
    }
    private string _bearerToken = string.Empty;
    public string BearerToken
    {
        get => _bearerToken;
        set => this.RaiseAndSetIfChanged(ref _bearerToken, value ?? string.Empty);
    }
    private string _authUsername = string.Empty;
    public string AuthUsername
    {
        get => _authUsername;
        set => this.RaiseAndSetIfChanged(ref _authUsername, value ?? string.Empty);
    }
    private string _authPassword = string.Empty;
    public string AuthPassword
    {
        get => _authPassword;
        set => this.RaiseAndSetIfChanged(ref _authPassword, value ?? string.Empty);
    }
    private string _apiKeyName = string.Empty;
    public string ApiKeyName
    {
        get => _apiKeyName;
        set => this.RaiseAndSetIfChanged(ref _apiKeyName, value ?? string.Empty);
    }
    private string _apiKeyValue = string.Empty;
    public string ApiKeyValue
    {
        get => _apiKeyValue;
        set => this.RaiseAndSetIfChanged(ref _apiKeyValue, value ?? string.Empty);
    }
    private int _apiKeyLocation = 0;
    public int ApiKeyLocation
    {
        get => _apiKeyLocation;
        set => this.RaiseAndSetIfChanged(ref _apiKeyLocation, value);
    }
    private bool _followRedirects = true;
    public bool FollowRedirects
    {
        get => _followRedirects;
        set => this.RaiseAndSetIfChanged(ref _followRedirects, value);
    }
    private bool _verifySslCertificate = true;
    public bool VerifySslCertificate
    {
        get => _verifySslCertificate;
        set => this.RaiseAndSetIfChanged(ref _verifySslCertificate, value);
    }
    private bool _automaticallyEncodeUrl = true;
    public bool AutomaticallyEncodeUrl
    {
        get => _automaticallyEncodeUrl;
        set => this.RaiseAndSetIfChanged(ref _automaticallyEncodeUrl, value);
    }
    private int _timeoutSeconds = 30;
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => this.RaiseAndSetIfChanged(ref _timeoutSeconds, value);
    }
    private bool _isSending = false;
    public bool IsSending
    {
        get => _isSending;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isSending, value);
            this.RaisePropertyChanged(nameof(ShowResponsePlaceholder));
            this.RaisePropertyChanged(nameof(HasResponseMessage));
        }
    }
    private string _responseText = string.Empty;
    public string ResponseText
    {
        get => _responseText;
        set => this.RaiseAndSetIfChanged(ref _responseText, value ?? string.Empty);
    }
    private string _responseStatusText = string.Empty;
    public string ResponseStatusText
    {
        get => _responseStatusText;
        set => this.RaiseAndSetIfChanged(ref _responseStatusText, value ?? string.Empty);
    }
    private string _responseTime = string.Empty;
    public string ResponseTime
    {
        get => _responseTime;
        set => this.RaiseAndSetIfChanged(ref _responseTime, value ?? string.Empty);
    }
    private string _responseSize = string.Empty;
    public string ResponseSize
    {
        get => _responseSize;
        set => this.RaiseAndSetIfChanged(ref _responseSize, value ?? string.Empty);
    }
    private bool _hasResponse = false;
    public bool HasResponse
    {
        get => _hasResponse;
        private set
        {
            this.RaiseAndSetIfChanged(ref _hasResponse, value);
            this.RaisePropertyChanged(nameof(ShowResponsePlaceholder));
            this.RaisePropertyChanged(nameof(HasResponseMessage));
        }
    }
    private bool _isResponseError = false;
    public bool IsResponseError
    {
        get => _isResponseError;
        private set => this.RaiseAndSetIfChanged(ref _isResponseError, value);
    }
    private string _responseMessage = string.Empty;
    public string ResponseMessage
    {
        get => _responseMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref _responseMessage, value ?? string.Empty);
            this.RaisePropertyChanged(nameof(ShowResponsePlaceholder));
            this.RaisePropertyChanged(nameof(HasResponseMessage));
        }
    }
    private string _responseDetails = string.Empty;
    public string ResponseDetails
    {
        get => _responseDetails;
        set => this.RaiseAndSetIfChanged(ref _responseDetails, value ?? string.Empty);
    }
    private string _headerBulkText = string.Empty;
    public string HeaderBulkText
    {
        get => _headerBulkText;
        set => this.RaiseAndSetIfChanged(ref _headerBulkText, value ?? string.Empty);
    }
    private bool _isHeaderBulkEdit = false;
    public bool IsHeaderBulkEdit
    {
        get => _isHeaderBulkEdit;
        private set => this.RaiseAndSetIfChanged(ref _isHeaderBulkEdit, value);
    }

    public Command CmdSend { get; }
    public Command CmdCancel { get; }
    public Command CmdAddQueryParam { get; }
    public Command CmdAddHeader { get; }
    public Command CmdAddFormDataParam { get; }
    public Command CmdClearResponse { get; }
    public Command CmdToggleHeaderBulkEdit { get; }
    public Command CmdImportQuery { get; }

    public HttpRequestModel BuildRequest()
    {
        if (IsHeaderBulkEdit) ApplyHeaderBulkEdit();
        return new HttpRequestModel
        {
            Method = Request.Method,
            Url = Request.Url,
            Body = Request.Body ?? string.Empty,
            BodyMode = (RequestBodyMode)SelectedBodyMode,
            BodyContentType = SelectedMediaType switch
            {
                "JSON" => "application/json",
                "XML" => "application/xml",
                "HTML" => "text/html",
                _ => "text/plain"
            },
            QueryParameters = QueryParams.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Key))
                .Select(p => new KeyValuePair<string, string>(p.Key!, p.Value ?? string.Empty)).ToList(),
            HeaderEntries = Headers.Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Key))
                .Select(p => new KeyValuePair<string, string>(p.Key!, p.Value ?? string.Empty)).ToList(),
            FormFields = FormDataParams.Select(p => new FormDataField
            { Key = p.Key, Value = p.Value, IsEnabled = p.IsEnabled, IsFile = p.IsFile }).ToList(),
            AuthType = SelectedAuthType,
            BearerToken = BearerToken,
            Username = AuthUsername,
            Password = AuthPassword,
            ApiKeyName = ApiKeyName,
            ApiKeyValue = ApiKeyValue,
            ApiKeyInQuery = ApiKeyLocation == 1,
            FollowRedirects = FollowRedirects,
            VerifySslCertificate = VerifySslCertificate,
            AutomaticallyEncodeUrl = AutomaticallyEncodeUrl,
            TimeoutSeconds = TimeoutSeconds
        };
    }

    public async Task SendAsync()
    {
        if (IsSending) return;
        IsSending = true;
        ClearResponse();
        ResponseStatusText = "Sending...";
        using var cancellation = new CancellationTokenSource();
        _requestCancellation = cancellation;
        try
        {
            var request = BuildRequest();
            // Keep request construction, TLS setup, decoding and transport continuations off the UI thread.
            var response = await Task.Run(() => _service.SendAsync(request, cancellation.Token), cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            _rawResponseBody = response.Body;
            _responseBytes = response.BodyBytes;
            _isBinaryResponse = response.IsBinary;
            ResponseContentType = response.ContentType;
            foreach (var header in response.Headers)
            {
                ResponseHeaders.Add(new() { Key = header.Key, Value = string.Join(", ", header.Value) });
                if (header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var cookie in header.Value) ResponseCookies.Add(ResponseCookie.Parse(cookie));
                }
            }
            HasResponse = true;
            IsResponseError = response.StatusCode >= 400;
            ResponseStatusText = $"{response.StatusCode} {response.ReasonPhrase}";
            ResponseTime = response.ElapsedMilliseconds + " ms";
            var bytes = response.BodyBytes.LongLength;
            ResponseSize = bytes >= 1024 * 1024 ? $"{bytes / (1024d * 1024):0.##} MB"
                : bytes >= 1024 ? $"{bytes / 1024d:0.##} KB" : $"{bytes} B";
            ResponseDetails = $"HTTP/{response.HttpVersion} | {response.ContentType} | {response.FinalUrl}";
            ResponseFormat = DetectFormat(response);
            await ResponseFormattingTask;
            cancellation.Token.ThrowIfCancellationRequested();
            this.RaisePropertyChanged(nameof(ResponseBytes));
            this.RaisePropertyChanged(nameof(IsImagePreview));
        }
        catch (OperationCanceledException)
        {
            ClearResponse();
            ResponseStatusText = "Cancelled";
            ResponseMessage = "The request was cancelled.";
        }
        catch (Exception ex)
        {
            IsResponseError = true;
            ResponseStatusText = ex is TimeoutException ? "Timed out" : "Could not send request";
            ResponseMessage = ex is HttpRequestException http && http.InnerException != null
                ? $"{http.Message}\n{http.InnerException.Message}" : ex.Message;
        }
        finally
        {
            _requestCancellation = null;
            IsSending = false;
        }
    }

    public void CancelRequest()
    {
        _requestCancellation?.Cancel();
        _formatCancellation?.Cancel();
    }

    private static string DetectFormat(HttpResponseModel response)
    {
        if (response.IsBinary) return "Binary";
        var type = response.ContentType.ToLowerInvariant();
        if (type.Contains("json")) return "JSON";
        if (type.Contains("html")) return "HTML";
        if (type.Contains("xml")) return "XML";
        var text = response.Body.AsSpan().TrimStart();
        return text.StartsWith("{") || text.StartsWith("[") ? "JSON" : "Text";
    }

    private async Task UpdateResponseBodyAsync()
    {
        _formatCancellation?.Cancel();
        _formatCancellation?.Dispose();
        _formatCancellation = null;
        var version = ++_formatVersion;
        if (!HasResponse) return;
        using var cancellation = new CancellationTokenSource();
        _formatCancellation = cancellation;
        var token = cancellation.Token;
        // Capture a consistent response and view selection before starting background work.
        var body = _rawResponseBody;
        var bytes = _responseBytes;
        var binary = _isBinaryResponse;
        var format = ResponseFormat;
        var mode = ResponseBodyMode;
        IsFormatting = true;
        this.RaisePropertyChanged(nameof(IsImagePreview));
        try
        {
            var display = await Task.Run(() => ResponseBodyFormatter.Format(body, bytes, binary, format, mode, token), token);
            if (version != _formatVersion || token.IsCancellationRequested) return;
            ResponseMessage = display.Message;
            ResponseText = display.Text;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (version == _formatVersion) ResponseMessage = $"Could not display the response: {ex.Message}";
        }
        finally
        {
            if (version == _formatVersion)
            {
                _formatCancellation = null;
                IsFormatting = false;
            }
        }
    }

    private void ClearResponse()
    {
        ++_formatVersion;
        _formatCancellation?.Cancel();
        _formatCancellation?.Dispose();
        _formatCancellation = null;
        _rawResponseBody = string.Empty;
        _responseBytes = Array.Empty<byte>();
        _isBinaryResponse = false;
        ResponseContentType = string.Empty;
        HasResponse = false;
        IsResponseError = false;
        ResponseHeaders.Clear();
        ResponseCookies.Clear();
        ResponseText = string.Empty;
        ResponseStatusText = string.Empty;
        ResponseTime = string.Empty;
        ResponseSize = string.Empty;
        ResponseMessage = string.Empty;
        ResponseDetails = string.Empty;
        IsFormatting = false;
        this.RaisePropertyChanged(nameof(ResponseBytes));
        this.RaisePropertyChanged(nameof(IsImagePreview));
    }

    private void ImportQuery()
    {
        var url = Request.Url ?? string.Empty;
        var hash = url.IndexOf('#');
        var fragment = hash >= 0 ? url[hash..] : string.Empty;
        var main = hash >= 0 ? url[..hash] : url;
        var question = main.IndexOf('?');
        if (question < 0) return;
        // Move URL parameters into editable rows once; keep duplicates and blank values.
        foreach (var segment in main[(question + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = segment.Split('=', 2);
            QueryParams.Add(new()
            {
                Key = System.Net.WebUtility.UrlDecode(pair[0]),
                Value = pair.Length > 1 ? System.Net.WebUtility.UrlDecode(pair[1]) : string.Empty
            });
        }
        Request.Url = main[..question] + fragment;
    }

    private void ToggleHeaderBulkEdit()
    {
        try
        {
            if (IsHeaderBulkEdit) ApplyHeaderBulkEdit();
            else HeaderBulkText = string.Join("\n", Headers.Where(h => !string.IsNullOrWhiteSpace(h.Key))
                .Select(h => $"{(h.IsEnabled ? "" : "# ")}{h.Key}: {h.Value}"));
            IsHeaderBulkEdit = !IsHeaderBulkEdit;
            ResponseMessage = string.Empty;
        }
        catch (ArgumentException ex) { ResponseMessage = ex.Message; }
    }

    private void ApplyHeaderBulkEdit()
    {
        var parsed = new List<KeyValueDto<string, string>>();
        foreach (var raw in HeaderBulkText.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            var enabled = !line.StartsWith('#');
            if (!enabled) line = line[1..].TrimStart();
            var colon = line.IndexOf(':');
            if (colon <= 0) throw new ArgumentException("Use one header per line in Key: Value format. Prefix disabled headers with #.");
            parsed.Add(new() { Key = line[..colon].Trim(), Value = line[(colon + 1)..].Trim(), IsEnabled = enabled });
        }
        Headers.Clear();
        foreach (var header in parsed) Headers.Add(header);
    }
}
