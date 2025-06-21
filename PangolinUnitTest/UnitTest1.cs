using System.Text;
using Pangolin.Utility;

namespace PangolinUnitTest;

public class Tests
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

    [Test]
    public void Test2()
    {
        string original = "哈哈Here is some text to encrypt!呵呵";
        // 必须是16, 24, 或32字节长
        byte[] key = Encoding.UTF8.GetBytes("1234567897777777");
        // 必须是16字节长
        byte[] iv = Encoding.UTF8.GetBytes("1234567897777777");

        // 加密
        byte[] encrypted = AesUtil.Encrypt(Encoding.UTF8.GetBytes(original), key, iv);
        Console.WriteLine($"Encrypted: {Convert.ToBase64String(encrypted)}");

        // 解密
        string decrypted = Encoding.UTF8.GetString(AesUtil.Decrypt(encrypted, key, iv));
        Console.WriteLine($"Decrypted: {decrypted}");

        Assert.True(original.Equals(decrypted));
    }
}