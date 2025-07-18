using System.Text;
using Pangolin.Utility;

namespace PangolinUnitTest;

public class TripleDesUtilTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        string plainText = "测试bbba12234djdjd*&^%$_数据";
        Console.WriteLine(Encoding.UTF8.GetBytes(plainText).Length);
        byte[] key = Encoding.UTF8.GetBytes(CommonUtil.Generate(16));
        byte[] iv = Encoding.UTF8.GetBytes(CommonUtil.Generate(8));
        // CipherMode[] cipherModes = Enum.GetValues<CipherMode>();
        // PaddingMode[] paddingModes = Enum.GetValues<PaddingMode>();

        foreach (var pair in TripleDesUtil.ValidCipherModePaddingModeDictionary)
        {
            foreach (var paddingMode in pair.Value)
            {
                Console.WriteLine($"开始处理 {pair.Key.ToString()} {paddingMode}");
                string cipherText = TripleDesUtil.Encrypt(Encoding.UTF8.GetBytes(plainText), key, iv, pair.Key, paddingMode);
                string output = TripleDesUtil.Decrypt(cipherText, key, iv, pair.Key, paddingMode);
                Assert.True(plainText.Equals(output));
            }
        }
    }
}