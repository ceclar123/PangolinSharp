using System.Security.Cryptography;
using System.Text;
using Pangolin.Utility;
using Pangolin.Utility.Des;

namespace PangolinUnitTest;

public class DesUtilTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        string plainText = "测试bbba12234djdjd*&^%$_数据";
        byte[] key = Encoding.UTF8.GetBytes(CommonUtil.Generate(8));
        byte[] iv = Encoding.UTF8.GetBytes(CommonUtil.Generate(8));

        byte[] cipherBytes = DesOfbUtil.Encrypt(Encoding.UTF8.GetBytes(plainText), key, iv, PaddingMode.PKCS7);
        string cipherText = Encoding.UTF8.GetString(cipherBytes);

        string output = DesOfbUtil.Decrypt(cipherBytes, key, iv, PaddingMode.PKCS7);
        Assert.True(plainText.Equals(output));
    }

    [Test]
    public void Test2()
    {
        string plainText = "测试bbba12234djdjd*&^%$_数据";
        byte[] key = Encoding.UTF8.GetBytes(CommonUtil.Generate(8));
        byte[] iv = Encoding.UTF8.GetBytes(CommonUtil.Generate(8));

        byte[] cipherBytes = DesCtsUtil.Encrypt(Encoding.UTF8.GetBytes(plainText), key, iv);
        string cipherText = Encoding.UTF8.GetString(cipherBytes);

        string output = DesCtsUtil.Decrypt(cipherBytes, key, iv);
        Assert.True(plainText.Equals(output));
    }

    [Test]
    public void Test3()
    {
        string plainText = "测试bbba12234djdjd*&^%$_数据";
        Console.WriteLine(Encoding.UTF8.GetBytes(plainText).Length);
        byte[] key = Encoding.UTF8.GetBytes(CommonUtil.Generate(8));
        byte[] iv = Encoding.UTF8.GetBytes(CommonUtil.Generate(8));
        // CipherMode[] cipherModes = Enum.GetValues<CipherMode>();
        // PaddingMode[] paddingModes = Enum.GetValues<PaddingMode>();

        foreach (var pair in DesUtil.ValidCipherModePaddingModeDictionary)
        {
            foreach (var paddingMode in pair.Value)
            {
                Console.WriteLine($"开始处理 {pair.Key.ToString()} {paddingMode}");
                string cipherText = DesUtil.Encrypt(plainText, key, iv, pair.Key, paddingMode);
                string output = DesUtil.Decrypt(cipherText, key, iv, pair.Key, paddingMode);
                Assert.True(plainText.Equals(output));
            }
        }
    }
}