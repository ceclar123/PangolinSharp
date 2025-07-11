using System.Net;
using Pangolin.Utility;
using RestSharp;

namespace PangolinUnitTest;

public class HttpUtilTest
{
    [SetUp]
    public void Setup()
    {
    }

    private void Print<T>(RestResponse<T> response)
    {
        Console.WriteLine($"status: {response.StatusCode}, desc: {response.StatusDescription}, size: {response.Content?.Length ?? 0}");
        Console.WriteLine("--------------------header-----------------------");
        if (response.Headers != null)
        {
            foreach (var headerParam in response.Headers)
            {
                Console.WriteLine($"{headerParam.Name}: {headerParam.Value}");
            }
        }

        Console.WriteLine("--------------------cookie-----------------------");
        if (response.Cookies != null)
        {
            foreach (Cookie cookie in response.Cookies)
            {
                Console.WriteLine($"{cookie.Name}: {cookie.Value}");
            }
        }

        Console.WriteLine("--------------------body-----------------------");
        Console.WriteLine(response.Content);
    }

    [Test]
    public void TestHead1()
    {
        RestResponse<string> response = HttpUtil.DoHead("https://t.aliyun.com/abs/search/fetchGuidingTerm?platform=PC&channel=1&pageSize=3&loc=2024NSPlaceholder");
        Print(response);
        Assert.IsTrue(HttpStatusCode.OK.Equals(response.StatusCode));
    }

    [Test]
    public void TestOptions1()
    {
        RestResponse<string> response = HttpUtil.DoOptions("https://t.aliyun.com/abs/search/fetchGuidingTerm?platform=PC&channel=1&pageSize=3&loc=2024NSPlaceholder");
        Print(response);
        Assert.IsTrue(HttpStatusCode.OK.Equals(response.StatusCode));
    }

    [Test]
    public void TestGet1()
    {
        RestResponse<string> response = HttpUtil.DoGet("https://t.aliyun.com/abs/search/fetchGuidingTerm?platform=PC&channel=1&pageSize=3&loc=2024NSPlaceholder");
        Print(response);
        Assert.IsTrue(HttpStatusCode.OK.Equals(response.StatusCode));
    }
}