using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pangolin.Desktop.Models.Postman;

namespace Pangolin.Desktop.Services;

public class HttpClientService
{
    private static readonly ConcurrentDictionary<(bool Redirect, bool VerifySsl), HttpClient> Clients = new();

    static HttpClientService() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public async Task<HttpResponseModel> SendAsync(HttpRequestModel request, CancellationToken cancellationToken = default)
    {
        using var message = CreateRequestMessage(request);
        var client = Clients.GetOrAdd((request.FollowRedirects, request.VerifySslCertificate), options =>
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = options.Redirect,
                MaxAutomaticRedirections = 10,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.All
            };
            if (!options.VerifySsl)
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        });
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            var bytes = await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(false);
            watch.Stop();
            var contentType = response.Content.Headers.ContentType;
            var binary = IsBinaryContent(contentType?.MediaType, bytes);
            var result = new HttpResponseModel
            {
                StatusCode = (int)response.StatusCode,
                ReasonPhrase = response.ReasonPhrase ?? string.Empty,
                BodyBytes = bytes,
                Body = binary ? string.Empty : DecodeBody(bytes, contentType?.CharSet),
                ContentType = contentType?.MediaType ?? string.Empty,
                IsBinary = binary,
                FinalUrl = response.RequestMessage?.RequestUri?.AbsoluteUri ?? request.Url,
                HttpVersion = response.Version.ToString(),
                ElapsedMilliseconds = watch.ElapsedMilliseconds
            };
            foreach (var header in response.Headers.Concat(response.Content.Headers))
                result.Headers[header.Key] = header.Value.ToArray();
            return result;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Request timed out after {request.TimeoutSeconds} seconds.", ex);
        }
    }

    public static HttpRequestMessage CreateRequestMessage(HttpRequestModel request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TimeoutSeconds is < 1 or > 3600)
            throw new ArgumentException("Timeout must be between 1 and 3600 seconds.");
        var headers = request.Headers.Concat(request.HeaderEntries).ToList();
        var query = request.QueryParameters.ToList();
        ApplyAuthorization(request, headers, query);
        var uri = BuildUri(request.Url, query, request.AutomaticallyEncodeUrl);
        var message = new HttpRequestMessage(new HttpMethod(request.Method.ToString()), uri);
        try
        {
            message.Content = CreateContent(request);
            foreach (var header in headers)
            {
                var name = header.Key.Trim();
                var value = header.Value;
                if (name.Length == 0 || name.Any(c => !char.IsAsciiLetterOrDigit(c) && !"!#$%&'*+-.^_`|~".Contains(c))
                    || value.Contains('\r') || value.Contains('\n'))
                    throw new ArgumentException($"Invalid HTTP header: {name}.");
                if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Content-Length is calculated automatically. Remove this header.");
                if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && request.BodyMode == RequestBodyMode.FormData)
                {
                    if (!MediaTypeHeaderValue.TryParse(value, out var media) || !media.MediaType!.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("form-data requires multipart/form-data. Remove the Content-Type header to generate it automatically.");
                    // MultipartFormDataContent owns the boundary, including when users supply Content-Type.
                    continue;
                }
                if (!message.Headers.TryAddWithoutValidation(name, value))
                {
                    // Content headers are also legal on an explicitly empty request.
                    message.Content ??= new ByteArrayContent(Array.Empty<byte>());
                    if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(value);
                    else if (!message.Content.Headers.TryAddWithoutValidation(name, value))
                        throw new ArgumentException($"Unsupported HTTP header: {name}.");
                }
            }
            return message;
        }
        catch { message.Dispose(); throw; }
    }

    public static Uri BuildUri(string url, IEnumerable<KeyValuePair<string, string>> parameters, bool encode)
    {
        url = (url ?? string.Empty).Trim();
        var hash = url.IndexOf('#');
        if (hash >= 0) url = url[..hash]; // Fragments never belong to an HTTP request target.
        foreach (var pair in parameters)
        {
            var key = encode ? Uri.EscapeDataString(pair.Key) : pair.Key;
            var value = encode ? Uri.EscapeDataString(pair.Value) : pair.Value;
            var separator = !url.Contains('?') ? "?" : url.EndsWith('?') || url.EndsWith('&') ? "" : "&";
            url += separator + key + "=" + value;
        }
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) || string.IsNullOrEmpty(parsed.Host))
            throw new ArgumentException("Enter a valid HTTP or HTTPS URL, including the scheme.");
        if (!string.IsNullOrEmpty(parsed.UserInfo))
            throw new ArgumentException("Use the Authorization tab instead of credentials in the URL.");
        if (url.Contains('\r') || url.Contains('\n')) throw new ArgumentException("URL cannot contain line breaks.");
        if (!encode && url.Any(c => c <= ' ' || c > 127))
            throw new ArgumentException("URL contains spaces or non-ASCII characters. Enable automatic URL encoding.");
        return encode ? parsed : new Uri(url, new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });
    }

    private static void ApplyAuthorization(HttpRequestModel request, List<KeyValuePair<string, string>> headers,
        List<KeyValuePair<string, string>> query)
    {
        void SetHeader(string name, string value)
        {
            headers.RemoveAll(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
            headers.Add(new(name, value));
        }
        switch (request.AuthType)
        {
            case "No Auth": break;
            case "Bearer Token":
                if (string.IsNullOrWhiteSpace(request.BearerToken)) throw new ArgumentException("Enter a bearer token.");
                SetHeader("Authorization", "Bearer " + request.BearerToken.Trim());
                break;
            case "Basic Auth":
                if (request.Username.Contains(':')) throw new ArgumentException("Basic Auth username cannot contain a colon.");
                SetHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Username + ":" + request.Password)));
                break;
            case "API Key":
                if (string.IsNullOrWhiteSpace(request.ApiKeyName)) throw new ArgumentException("Enter an API key name.");
                if (request.ApiKeyInQuery) query.Add(new(request.ApiKeyName.Trim(), request.ApiKeyValue));
                else SetHeader(request.ApiKeyName.Trim(), request.ApiKeyValue);
                break;
            default: throw new ArgumentException("Unsupported authorization type.");
        }
    }

    private static HttpContent? CreateContent(HttpRequestModel request)
    {
        switch (request.BodyMode)
        {
            case RequestBodyMode.None: return null;
            case RequestBodyMode.Raw:
                return new StringContent(request.Body, Encoding.UTF8, request.BodyContentType);
            case RequestBodyMode.UrlEncoded:
                if (request.FormFields.Any(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Key) && f.IsFile))
                    throw new ArgumentException("File fields require form-data mode. Switch the body mode or uncheck File.");
                return new FormUrlEncodedContent(request.FormFields.Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Key))
                    .Select(f => new KeyValuePair<string, string>(f.Key!, f.Value ?? string.Empty)));
            case RequestBodyMode.FormData:
                var multipart = new MultipartFormDataContent();
                try
                {
                    foreach (var field in request.FormFields.Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Key)))
                    {
                        if (field.Key!.Contains('\r') || field.Key.Contains('\n'))
                            throw new ArgumentException("Form field names cannot contain line breaks.");
                        if (field.IsFile)
                        {
                            var path = field.Value ?? string.Empty;
                            if (!File.Exists(path)) throw new FileNotFoundException($"Upload file does not exist: {path}");
                            var content = new StreamContent(File.OpenRead(path));
                            try
                            {
                                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                                multipart.Add(content, field.Key!, Path.GetFileName(path));
                            }
                            catch { content.Dispose(); throw; }
                        }
                        else multipart.Add(new StringContent(field.Value ?? string.Empty, Encoding.UTF8), field.Key!);
                    }
                    return multipart;
                }
                catch { multipart.Dispose(); throw; }
            default: throw new ArgumentException("Unsupported request body mode.");
        }
    }

    private static bool IsBinaryContent(string? mediaType, byte[] bytes)
    {
        if (mediaType != null)
        {
            mediaType = mediaType.ToLowerInvariant();
            if (mediaType.StartsWith("text/") || mediaType.Contains("json") || mediaType.Contains("xml") ||
                mediaType.Contains("javascript") || mediaType == "application/x-www-form-urlencoded") return false;
            return true;
        }
        // UTF-16/32 BOMs can contain zero bytes and are still text.
        if (bytes.Length >= 2 && ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF))) return false;
        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xFE && bytes[3] == 0xFF) return false;
        return bytes.Take(512).Any(b => b == 0);
    }

    private static string DecodeBody(byte[] bytes, string? charset)
    {
        var encoding = Encoding.UTF8;
        if (!string.IsNullOrWhiteSpace(charset))
        {
            try { encoding = Encoding.GetEncoding(charset.Trim('"')); }
            catch (ArgumentException) { /* Unknown charset: fall back to UTF-8 and BOM detection. */ }
        }
        using var reader = new StreamReader(new MemoryStream(bytes), encoding, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
