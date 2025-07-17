using System.Text;
using Pangolin.Utility;

namespace PangolinUnitTest;

public class UnitTests1
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        string src = "this is a test case";
        byte[] data = GzipUtil.Compress(Encoding.UTF8.GetBytes(src));
        string output = Base64Util.Encode(data);
        //string output = GzipUtil.CompressAndBase64Encode(src);
        Console.WriteLine("encode -> " + output);

        data = Base64Util.Decode(output);
        string dest = Encoding.UTF8.GetString(GzipUtil.Decompress(data));
        //string dest = GzipUtil.DecompressBase64Decode(output);
        Console.WriteLine("decode -> " + dest);

        Assert.True(src == dest);
    }
}