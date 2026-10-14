using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;

namespace Pangolin.Desktop.Services;

public sealed record ResponseDisplay(string Text, string Message);

// Called on a worker thread. Only a bounded preview reaches the desktop text renderer.
public static class ResponseBodyFormatter
{
    public const int DisplayLimit = 16_000;
    public const int LineLimit = 500;
    private const int FormatLimit = 256_000;

    public static ResponseDisplay Format(string body, byte[] bytes, bool binary, string format, int mode, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (format == "Binary")
        {
            var length = Math.Min(bytes.Length, 4096);
            var hex = new StringBuilder();
            for (var offset = 0; offset < length; offset += 16)
                hex.AppendLine($"{offset:X8}  {Convert.ToHexString(bytes.AsSpan(offset, Math.Min(16, length - offset)))}");
            return new(hex.ToString(), $"Response bytes ({bytes.Length:N0} bytes). Save downloads the original bytes."
                + (bytes.Length > length ? " Showing the first 4096 bytes." : string.Empty));
        }
        var text = binary ? Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, DisplayLimit * 4)) : body;
        var message = binary ? "Showing binary content as UTF-8 text." : string.Empty;
        if (mode == 0 && !string.IsNullOrWhiteSpace(text) && text.Length <= FormatLimit)
        {
            try
            {
                if (format == "JSON")
                {
                    using var json = JsonDocument.Parse(text);
                    text = JsonSerializer.Serialize(json.RootElement, new JsonSerializerOptions
                    { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                }
                else if (format == "XML") text = XDocument.Parse(text).ToString();
            }
            catch (Exception ex) when (ex is JsonException or System.Xml.XmlException)
            {
                message = $"Cannot format as {format}; showing the original text.";
            }
        }
        cancellation.ThrowIfCancellationRequested();
        var end = Math.Min(text.Length, DisplayLimit);
        var lines = 1;
        for (var i = 0; i < end; i++)
            if (text[i] == '\n' && ++lines > LineLimit) { end = i; break; }
        if (end < text.Length || (binary && bytes.Length > DisplayLimit * 4))
        {
            if (end > 0 && char.IsHighSurrogate(text[end - 1])) end--;
            text = text[..end];
            message = $"Showing a preview (up to {DisplayLimit:N0} characters / {LineLimit:N0} lines). Save downloads the complete response.";
        }
        return new(text, message);
    }
}
