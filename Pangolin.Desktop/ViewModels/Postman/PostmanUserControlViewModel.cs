using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pangolin.Desktop.Models;
using Pangolin.Desktop.Models.Postman;
using Pangolin.Desktop.Services;
using Pangolin.SQLite;
using Pangolin.SQLite.Models;
using ReactiveUI;
using Command = ReactiveUI.ReactiveCommand<ReactiveUI.Primitives.RxVoid, ReactiveUI.Primitives.RxVoid>;

namespace Pangolin.Desktop.ViewModels.Postman;

public class PostmanUserControlViewModel : ViewModelBase
{
    private readonly HttpClientService _service = new();
    private readonly RequestHistoryStore _historyStore;
    private readonly Task _historyInitialization;
    private int _historyLoadVersion;
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

    public PostmanUserControlViewModel() : this(new RequestHistoryStore()) { }

    public PostmanUserControlViewModel(RequestHistoryStore historyStore)
    {
        _historyStore = historyStore;
        _historyInitialization = InitializeHistoryAsync();
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
        CmdShowCollections = ReactiveCommand.CreateFromTask(ShowCollectionsAsync);
        CmdShowHistory = ReactiveCommand.CreateFromTask(ShowHistoryAsync);
    }

    public HttpRequestModel Request { get; } = new();
    public ObservableCollection<RequestHistorySummary> HistoryEntries { get; } = new();
    public ObservableCollection<RequestHistorySummary> VisibleHistoryEntries { get; } = new();
    public ObservableCollection<RequestCollectionNode> CollectionRoots { get; } = new();
    public ObservableCollection<RequestHistorySummary> CollectionHistoryEntries { get; } = new();
    private List<RequestCollectionNode> _allCollectionNodes = new();
    public IReadOnlyList<RequestCollectionNode> AllCollectionNodes => _allCollectionNodes;
    public string DatabasePath => _historyStore.DatabasePath;
    public bool IsCollectionView => !IsHistoryView;
    private bool _isHistoryView;
    public bool IsHistoryView
    {
        get => _isHistoryView;
        set
        {
            this.RaiseAndSetIfChanged(ref _isHistoryView, value);
            this.RaisePropertyChanged(nameof(IsCollectionView));
        }
    }
    private string _historyFilter = string.Empty;
    public string HistoryFilter
    {
        get => _historyFilter;
        set
        {
            this.RaiseAndSetIfChanged(ref _historyFilter, value ?? string.Empty);
            this.RaisePropertyChanged(nameof(HistoryEmptyText));
            FilterHistory();
        }
    }
    private string _historyError = string.Empty;
    public string HistoryError
    {
        get => _historyError;
        private set
        {
            this.RaiseAndSetIfChanged(ref _historyError, value ?? string.Empty);
            this.RaisePropertyChanged(nameof(HasHistoryError));
        }
    }
    public void ReportHistoryError(string message) => HistoryError = message;
    public bool HasHistory => HistoryEntries.Count != 0;
    public bool HasVisibleHistory => VisibleHistoryEntries.Count != 0;
    public bool HasHistoryError => !string.IsNullOrWhiteSpace(HistoryError);
    public string HistoryEmptyText => HistoryEntries.Count == 0 ? "No request history yet" : "No matching requests";
    public bool HistorySecretsRedacted { get; private set; }
    public string HistorySecretsNotice => HistorySecretsRedacted
        ? "Auth secrets, sensitive headers/parameters, and upload paths were omitted. Raw body text is stored as entered. Review the request before sending."
        : string.Empty;
    private RequestHistorySummary? _selectedHistoryItem;
    public Task HistoryRestoreTask { get; private set; } = Task.CompletedTask;
    public RequestHistorySummary? SelectedHistoryItem
    {
        get => _selectedHistoryItem;
        set
        {
            if (ReferenceEquals(_selectedHistoryItem, value)) return;
            this.RaiseAndSetIfChanged(ref _selectedHistoryItem, value);
            this.RaisePropertyChanged(nameof(HasSelectedHistoryItem));
        }
    }
    private RequestCollectionNode? _selectedCollectionNode;
    public RequestCollectionNode? SelectedCollectionNode
    {
        get => _selectedCollectionNode;
        set
        {
            if (ReferenceEquals(_selectedCollectionNode, value)) return;
            this.RaiseAndSetIfChanged(ref _selectedCollectionNode, value);
            this.RaisePropertyChanged(nameof(HasSelectedCollection));
            this.RaisePropertyChanged(nameof(CanCreateChildCollection));
            this.RaisePropertyChanged(nameof(CanAddHistoryToSelectedCollection));
            this.RaisePropertyChanged(nameof(ShowEmptyCollectionMessage));
            for (var parent = value?.Parent; parent != null; parent = parent.Parent) parent.IsExpanded = true;
            if (value == null)
            {
                CollectionHistoryEntries.Clear();
                this.RaisePropertyChanged(nameof(HasCollectionHistory));
            }
            else CollectionHistoryLoadTask = LoadCollectionHistoryAsync(value.Id);
        }
    }
    public Task CollectionHistoryLoadTask { get; private set; } = Task.CompletedTask;
    public bool HasSelectedHistoryItem => SelectedHistoryItem != null;
    public bool HasSelectedCollection => SelectedCollectionNode != null;
    public bool CanCreateChildCollection => SelectedCollectionNode is { Depth: < 2 };
    public bool CanAddHistoryToSelectedCollection => SelectedCollectionNode is { Id: > 0 };
    public bool HasCollectionHistory => CollectionHistoryEntries.Count != 0;
    public bool ShowEmptyCollectionMessage => HasSelectedCollection && !HasCollectionHistory;
    public bool HasCollections => CollectionRoots.Count != 0;
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
    public Command CmdShowCollections { get; }
    public Command CmdShowHistory { get; }

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

    private async Task InitializeHistoryAsync()
    {
        try
        {
            await _historyStore.InitializeAsync();
            await RefreshHistoryAsync();
            await RefreshCollectionsAsync();
        }
        catch (Exception ex)
        {
            HistoryError = $"Could not open request history: {ex.Message}";
        }
    }

    private async Task ShowHistoryAsync()
    {
        IsHistoryView = true;
        await _historyInitialization;
        await RefreshHistoryAsync();
    }

    private async Task ShowCollectionsAsync()
    {
        IsHistoryView = false;
        await _historyInitialization;
        await RefreshCollectionsAsync();
    }

    private async Task RefreshHistoryAsync()
    {
        try
        {
            var recent = await _historyStore.GetRecentAsync(RequestHistoryStore.MaximumHistoryEntries);
            HistoryEntries.Clear();
            foreach (var entry in recent) HistoryEntries.Add(entry);
            this.RaisePropertyChanged(nameof(HasHistory));
            this.RaisePropertyChanged(nameof(HistoryEmptyText));
            FilterHistory();
            HistoryError = string.Empty;
        }
        catch (Exception ex)
        {
            HistoryError = $"Could not load request history: {ex.Message}";
        }
    }

    private async Task RefreshCollectionsAsync(long? preferredSelection = null)
    {
        try
        {
            var selectionId = preferredSelection ?? SelectedCollectionNode?.Id;
            var collections = await _historyStore.GetCollectionsAsync();
            var byParent = collections.ToLookup(collection => collection.ParentId);
            CollectionRoots.Clear();
            _allCollectionNodes = new List<RequestCollectionNode>();

            void AddChildren(ObservableCollection<RequestCollectionNode> target, long? parentId, int depth, string parentPath, HashSet<long> ancestors, RequestCollectionNode? parentNode)
            {
                if (depth >= 3) return;
                foreach (var collection in byParent[parentId])
                {
                    if (!ancestors.Add(collection.Id)) continue;
                    var path = string.IsNullOrEmpty(parentPath) ? collection.Name : $"{parentPath} / {collection.Name}";
                    var node = new RequestCollectionNode(collection, depth, path);
                    node.Parent = parentNode;
                    target.Add(node);
                    _allCollectionNodes.Add(node);
                    AddChildren(node.Children, node.Id, depth + 1, path, ancestors, node);
                    ancestors.Remove(collection.Id);
                }
            }

            AddChildren(CollectionRoots, null, 0, string.Empty, new HashSet<long>(), null);
            this.RaisePropertyChanged(nameof(AllCollectionNodes));
            this.RaisePropertyChanged(nameof(HasCollections));
            SelectedCollectionNode = selectionId.HasValue
                ? _allCollectionNodes.FirstOrDefault(node => node.Id == selectionId)
                : null;
            HistoryError = string.Empty;
        }
        catch (Exception ex)
        {
            HistoryError = $"Could not load collections: {ex.Message}";
        }
    }

    public async Task CreateCollectionAsync(string name, long? parentId)
    {
        try
        {
            await _historyInitialization;
            if (parentId.HasValue && _allCollectionNodes.FirstOrDefault(node => node.Id == parentId) is { Depth: >= 2 })
                throw new InvalidOperationException("The desktop UI supports up to three collection levels.");
            var id = await _historyStore.CreateCollectionAsync(name, parentId);
            await RefreshCollectionsAsync(id);
        }
        catch (Exception ex) { HistoryError = $"Could not create collection: {ex.Message}"; }
    }

    public async Task RenameCollectionAsync(long id, string name)
    {
        try
        {
            await _historyInitialization;
            await _historyStore.RenameCollectionAsync(id, name);
            await RefreshCollectionsAsync(id);
        }
        catch (Exception ex) { HistoryError = $"Could not rename collection: {ex.Message}"; }
    }

    public async Task DeleteCollectionAsync(long id)
    {
        try
        {
            await _historyInitialization;
            var parentId = _allCollectionNodes.FirstOrDefault(node => node.Id == id)?.ParentId;
            await _historyStore.DeleteCollectionAsync(id);
            await RefreshCollectionsAsync(parentId);
        }
        catch (Exception ex) { HistoryError = $"Could not delete collection: {ex.Message}"; }
    }

    private async Task LoadCollectionHistoryAsync(long collectionId)
    {
        try
        {
            var items = await _historyStore.GetCollectionHistoryAsync(collectionId);
            if (SelectedCollectionNode?.Id != collectionId) return;
            CollectionHistoryEntries.Clear();
            foreach (var item in items) CollectionHistoryEntries.Add(item);
            this.RaisePropertyChanged(nameof(HasCollectionHistory));
            this.RaisePropertyChanged(nameof(ShowEmptyCollectionMessage));
            HistoryError = string.Empty;
        }
        catch (Exception ex) { HistoryError = $"Could not load collection requests: {ex.Message}"; }
    }

    public Task<IReadOnlyList<long>> GetHistoryCollectionIdsAsync(long historyId)
        => _historyStore.GetHistoryCollectionIdsAsync(historyId);

    public Task<IReadOnlyList<RequestHistorySummary>> GetCollectionPickerHistoryAsync()
        => _historyStore.GetRecentAsync(RequestHistoryStore.MaximumHistoryEntries);

    public Task<IReadOnlyList<RequestHistorySummary>> GetCollectionHistoryAsync(long collectionId)
        => _historyStore.GetCollectionHistoryAsync(collectionId);

    public async Task SetHistoryCollectionsAsync(long historyId, IEnumerable<long> collectionIds)
    {
        try
        {
            await _historyStore.SetHistoryCollectionsAsync(historyId, collectionIds);
            await RefreshCollectionsAsync();
            HistoryError = string.Empty;
        }
        catch (Exception ex) { HistoryError = $"Could not update collection membership: {ex.Message}"; }
    }

    public async Task SetCollectionHistoryMembersAsync(long collectionId, IEnumerable<long> historyIds)
    {
        try
        {
            await _historyStore.SetCollectionHistoryMembersAsync(collectionId, historyIds);
            await RefreshCollectionsAsync(collectionId);
            HistoryError = string.Empty;
        }
        catch (Exception ex) { HistoryError = $"Could not update collection requests: {ex.Message}"; }
    }

    private void FilterHistory()
    {
        VisibleHistoryEntries.Clear();
        var filter = HistoryFilter.Trim();
        foreach (var entry in HistoryEntries.Where(entry => string.IsNullOrEmpty(filter)
                     || entry.Method.Contains(filter, StringComparison.OrdinalIgnoreCase)
                     || entry.Url.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            VisibleHistoryEntries.Add(entry);
        this.RaisePropertyChanged(nameof(HasVisibleHistory));
    }

    private async Task SaveRequestHistoryAsync(HttpRequestModel request, RequestHistorySnapshot snapshot)
    {
        try
        {
            await _historyInitialization;
            var safeUrl = RedactUrl(request.Url);
            if (!string.Equals(safeUrl, request.Url, StringComparison.Ordinal)) snapshot.SecretsRedacted = true;
            var createdAtUtc = DateTimeOffset.UtcNow;
            var id = await _historyStore.SaveAsync(createdAtUtc, request.Method.ToString(), safeUrl, snapshot);
            var summary = new RequestHistorySummary
            {
                Id = id,
                CreatedAtUtc = createdAtUtc,
                Method = request.Method.ToString(),
                Url = safeUrl
            };
            HistoryEntries.Insert(0, summary);
            while (HistoryEntries.Count > RequestHistoryStore.MaximumHistoryEntries) HistoryEntries.RemoveAt(HistoryEntries.Count - 1);
            this.RaisePropertyChanged(nameof(HasHistory));
            this.RaisePropertyChanged(nameof(HistoryEmptyText));
            FilterHistory();
            await RefreshCollectionsAsync();
            HistoryError = string.Empty;
        }
        catch (Exception ex)
        {
            HistoryError = $"Request sent, but history could not be saved: {ex.Message}";
        }
    }

    private RequestHistorySnapshot CreateHistorySnapshot(HttpRequestModel request)
    {
        var redacted = false;
        var headers = Headers.Where(header => !string.IsNullOrWhiteSpace(header.Key)).Select(header =>
        {
            var hide = IsSensitiveName(header.Key);
            redacted |= hide && !string.IsNullOrEmpty(header.Value);
            return new RequestHistoryHeader
            {
                Name = header.Key!,
                Value = hide ? string.Empty : header.Value ?? string.Empty,
                IsEnabled = header.IsEnabled,
                WasRedacted = hide
            };
        }).ToList();

        var query = QueryParams.Where(parameter => !string.IsNullOrWhiteSpace(parameter.Key)).Select(parameter =>
        {
            var hide = IsSensitiveName(parameter.Key);
            redacted |= hide && !string.IsNullOrEmpty(parameter.Value);
            return new RequestHistoryParameter
            {
                Key = parameter.Key!,
                Value = hide ? string.Empty : parameter.Value ?? string.Empty,
                IsEnabled = parameter.IsEnabled,
                WasRedacted = hide
            };
        }).ToList();
        var fields = request.BodyMode is RequestBodyMode.FormData or RequestBodyMode.UrlEncoded
            ? FormDataParams.Where(field => !string.IsNullOrWhiteSpace(field.Key)).Select(field =>
        {
            var hide = field.IsFile || IsSensitiveName(field.Key);
            redacted |= hide && !string.IsNullOrEmpty(field.Value);
            return new RequestHistoryFormField
            {
                Key = field.Key ?? string.Empty,
                Value = hide ? string.Empty : field.Value ?? string.Empty,
                FileName = field.IsFile ? Path.GetFileName(field.Value) ?? string.Empty : string.Empty,
                IsEnabled = field.IsEnabled,
                IsFile = field.IsFile,
                WasRedacted = hide
            };
        }).ToList()
            : new List<RequestHistoryFormField>();

        var auth = new RequestHistoryAuthConfiguration
        {
            ApiKeyName = request.ApiKeyName,
            ApiKeyLocation = request.ApiKeyInQuery ? 1 : 0
        };
        if (request.AuthType == "Bearer Token") redacted |= !string.IsNullOrEmpty(request.BearerToken);
        if (request.AuthType == "Basic Auth") redacted |= !string.IsNullOrEmpty(request.Password);
        if (request.AuthType == "API Key") redacted |= !string.IsNullOrEmpty(request.ApiKeyValue);
        auth.Username = request.Username;

        return new RequestHistorySnapshot
        {
            Headers = headers,
            QueryParameters = query,
            BodyMode = request.BodyMode.ToString(),
            BodyContentType = request.BodyContentType,
            BodyText = request.BodyMode == RequestBodyMode.Raw ? request.Body : string.Empty,
            FormFields = fields,
            AuthType = request.AuthType,
            Auth = auth,
            Settings = new RequestHistorySettings
            {
                FollowRedirects = request.FollowRedirects,
                VerifySslCertificate = request.VerifySslCertificate,
                AutomaticallyEncodeUrl = request.AutomaticallyEncodeUrl,
                TimeoutSeconds = request.TimeoutSeconds
            },
            SecretsRedacted = redacted
        };
    }

    private async Task RestoreHistoryAsync(RequestHistorySummary summary)
    {
        var version = ++_historyLoadVersion;
        try
        {
            var detail = await _historyStore.GetDetailAsync(summary.Id);
            if (version != _historyLoadVersion || SelectedHistoryItem?.Id != summary.Id) return;
            if (detail == null)
            {
                HistoryError = "This request has no saved detail and cannot be restored.";
                return;
            }
            ClearResponse();
            Request.Method = Enum.TryParse<HttpMethodType>(summary.Method, out var method) ? method : HttpMethodType.GET;
            Request.Url = summary.Url;
            QueryParams.Clear();
            foreach (var parameter in detail.QueryParameters)
            {
                QueryParams.Add(new KeyValueDto<string, string> { Key = parameter.Key, Value = parameter.Value, IsEnabled = parameter.IsEnabled });
            }

            // header信息
            IsHeaderBulkEdit = false;
            HeaderBulkText = string.Empty;
            Headers.Clear();
            foreach (var header in detail.Headers)
            {
                Headers.Add(new KeyValueDto<string, string> { Key = header.Name, Value = header.Value, IsEnabled = header.IsEnabled });
            }
            FormDataParams.Clear();
            foreach (var field in detail.FormFields)
            {
                FormDataParams.Add(new FormDataField
                {
                    Key = field.Key,
                    Value = field.IsFile ? string.Empty : field.Value,
                    HistoryFileName = field.FileName,
                    IsEnabled = field.IsEnabled,
                    IsFile = field.IsFile
                });
            }
            SelectedBodyMode = Enum.TryParse<RequestBodyMode>(detail.BodyMode, out var bodyMode) ? (int)bodyMode : 0;
            SelectedMediaType = detail.BodyContentType switch
            {
                "application/json" => "JSON",
                "application/xml" => "XML",
                "text/html" => "HTML",
                _ => "Text"
            };
            Request.Body = detail.BodyText;
            SelectedAuthType = detail.AuthType;
            BearerToken = detail.Auth.BearerToken;
            AuthUsername = detail.Auth.Username;
            AuthPassword = detail.Auth.Password;
            ApiKeyName = detail.Auth.ApiKeyName;
            ApiKeyValue = detail.Auth.ApiKeyValue;
            ApiKeyLocation = detail.Auth.ApiKeyLocation;
            FollowRedirects = detail.Settings.FollowRedirects;
            VerifySslCertificate = detail.Settings.VerifySslCertificate;
            AutomaticallyEncodeUrl = detail.Settings.AutomaticallyEncodeUrl;
            TimeoutSeconds = detail.Settings.TimeoutSeconds;
            HistorySecretsRedacted = detail.SecretsRedacted;
            this.RaisePropertyChanged(nameof(HistorySecretsRedacted));
            this.RaisePropertyChanged(nameof(HistorySecretsNotice));
            if (HistorySecretsRedacted)
                MainNotificationManager?.Show(new Avalonia.Controls.Notifications.Notification(
                    "Request history", HistorySecretsNotice, Avalonia.Controls.Notifications.NotificationType.Information));
            HistoryError = string.Empty;
        }
        catch (Exception ex)
        {
            HistoryError = $"Could not restore request: {ex.Message}";
        }
    }

    public Task OpenSelectedHistoryAsync()
    {
        if (SelectedHistoryItem == null) return Task.CompletedTask;
        HistoryRestoreTask = RestoreHistoryAsync(SelectedHistoryItem);
        return HistoryRestoreTask;
    }

    private static bool IsSensitiveName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var normalized = new string(name.Where(char.IsLetterOrDigit).ToArray());
        //return normalized.Contains("password", StringComparison.OrdinalIgnoreCase)
        //    || normalized.Contains("passwd", StringComparison.OrdinalIgnoreCase)
        //    || normalized.Contains("secret", StringComparison.OrdinalIgnoreCase)
        //    || normalized.Contains("token", StringComparison.OrdinalIgnoreCase)
        //    || normalized.Contains("credential", StringComparison.OrdinalIgnoreCase)
        //    || normalized.Contains("authorization", StringComparison.OrdinalIgnoreCase)
        //    || normalized.Contains("apikey", StringComparison.OrdinalIgnoreCase)
        //    || normalized.Contains("privatekey", StringComparison.OrdinalIgnoreCase)
        //    || normalized is "auth" or "cookie" or "session" or "sessionid" or "setcookie";
        return false;
    }

    private static string RedactUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri) && !string.IsNullOrEmpty(absoluteUri.UserInfo))
        {
            url = new UriBuilder(absoluteUri) { UserName = string.Empty, Password = string.Empty }.Uri.AbsoluteUri;
        }
        var fragmentIndex = url.IndexOf('#');
        var fragment = fragmentIndex >= 0 ? url[fragmentIndex..] : string.Empty;
        var main = fragmentIndex >= 0 ? url[..fragmentIndex] : url;
        var queryIndex = main.IndexOf('?');
        if (queryIndex < 0) return url;
        var segments = main[(queryIndex + 1)..].Split('&');
        for (var i = 0; i < segments.Length; i++)
        {
            var equalsIndex = segments[i].IndexOf('=');
            var key = equalsIndex < 0 ? segments[i] : segments[i][..equalsIndex];
            string decodedKey;
            try { decodedKey = System.Net.WebUtility.UrlDecode(key); }
            catch (UriFormatException) { decodedKey = key; }
            if (IsSensitiveName(decodedKey)) segments[i] = key + "=";
        }
        return main[..(queryIndex + 1)] + string.Join("&", segments) + fragment;
    }

    public async Task DeleteHistoryAsync(long id)
        => await DeleteRequestHistoryAsync(id);

    public async Task DeleteHistoryDetailAsync(long id)
    {
        try
        {
            await _historyInitialization;
            await _historyStore.DeleteDetailAsync(id);
            if (SelectedHistoryItem?.Id == id)
            {
                ++_historyLoadVersion;
                SelectedHistoryItem = null;
            }
            await RefreshHistoryAsync();
        }
        catch (Exception ex)
        {
            HistoryError = $"Could not delete request history detail: {ex.Message}";
        }
    }

    public async Task DeleteRequestHistoryAsync(long id)
    {
        try
        {
            await _historyInitialization;
            await _historyStore.DeleteAsync(id);
            if (SelectedHistoryItem?.Id == id)
            {
                ++_historyLoadVersion;
                SelectedHistoryItem = null;
            }
            await RefreshHistoryAsync();
            await RefreshCollectionsAsync();
        }
        catch (Exception ex)
        {
            HistoryError = $"Could not delete request history: {ex.Message}";
        }
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
            var historySnapshot = CreateHistorySnapshot(request);
            await SaveRequestHistoryAsync(request, historySnapshot);
            cancellation.Token.ThrowIfCancellationRequested();
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
