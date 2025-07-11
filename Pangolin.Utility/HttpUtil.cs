using System.Net;
using RestSharp;

namespace Pangolin.Utility;

public static class HttpUtil
{
    public static RestResponse<string> DoHead(string url)
    {
        var options = new RestClientOptions();
        options.Timeout = TimeSpan.FromSeconds(10);
        var client = new RestClient(options);

        var request = new RestRequest(url, Method.Head);
        request.Version = HttpVersion.Version11;

        return client.Execute<string>(request);
    }

    public static RestResponse<string> DoOptions(string url)
    {
        var options = new RestClientOptions();
        options.Timeout = TimeSpan.FromSeconds(10);
        var client = new RestClient(options);

        var request = new RestRequest(url, Method.Options);
        request.Version = HttpVersion.Version11;

        return client.Execute<string>(request);
    }

    public static RestResponse<string> DoGet(string url)
    {
        var options = new RestClientOptions();
        options.Timeout = TimeSpan.FromSeconds(10);
        var client = new RestClient(options);

        var request = new RestRequest(url, Method.Get);
        request.Version = HttpVersion.Version11;

        return client.Execute<string>(request);
    }

    public static RestResponse<string> Do(RestRequest request)
    {
        var options = new RestClientOptions();
        options.Timeout = TimeSpan.FromSeconds(10);
        var client = new RestClient(options);

        return client.Execute<string>(request);
    }
}