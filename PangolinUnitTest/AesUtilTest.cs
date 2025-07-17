using System.Security.Cryptography;
using System.Text;
using Pangolin.Utility;

namespace PangolinUnitTest;

public class AesUtilTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        string plainText = "测试bbbbaaa12234djdjd*&^%$_数据";
        byte[] key = Encoding.UTF8.GetBytes(CommonUtil.Generate(16));
        byte[] iv = Encoding.UTF8.GetBytes(CommonUtil.Generate(16));

        string cipherText = AesUtil.Encrypt(plainText, key, iv, CipherMode.CBC, PaddingMode.PKCS7);

        string output = AesUtil.Decrypt(cipherText, key, iv, CipherMode.CBC, PaddingMode.PKCS7);
        Assert.True(plainText.Equals(output));
    }


    [Test]
    public void Test2()
    {
        string plainText = "测试bbba12234djdjd*&^%$_数据";
        Console.WriteLine(Encoding.UTF8.GetBytes(plainText).Length);
        byte[] key = Encoding.UTF8.GetBytes(CommonUtil.Generate(16));
        byte[] iv = Encoding.UTF8.GetBytes(CommonUtil.Generate(16));
        // CipherMode[] cipherModes = Enum.GetValues<CipherMode>();
        // PaddingMode[] paddingModes = Enum.GetValues<PaddingMode>();

        foreach (var pair in AesUtil.ValidCipherModePaddingModeDictionary)
        {
            foreach (var paddingMode in pair.Value)
            {
                Console.WriteLine($"开始处理 {pair.Key.ToString()} {paddingMode}");
                string cipherText = AesUtil.Encrypt(plainText, key, iv, pair.Key, paddingMode);
                string output = AesUtil.Decrypt(cipherText, key, iv, pair.Key, paddingMode);
                Assert.True(plainText.Equals(output));
            }
        }
    }
}