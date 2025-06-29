using System.Text;
using Pangolin.Utility;

namespace PangolinUnitTest;

public class Base64UnitTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        string input = "123#\uffe5……&张三李四122&……，。83";
        string output = Base64Util.Encode(Encoding.UTF8.GetBytes(input));
        Assert.True(input == Encoding.UTF8.GetString(Base64Util.Decode(output)));
    }

    [Test]
    public void Test12()
    {
        string input = "123#\uffe5……&张三李四122&……，。83";
        string output = Base64Util.UrlSafeEncode(Encoding.UTF8.GetBytes(input));
        Assert.True(input == Encoding.UTF8.GetString(Base64Util.UrlSafeDecode(output)));
    }
}