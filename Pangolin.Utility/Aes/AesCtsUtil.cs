using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Pangolin.Utility.Aes;

public static class AesCtsUtil
{
    public static byte[] Encrypt(byte[] plainBytes, byte[] key, byte[] iv)
    {
        // 创建 AES 引擎
        IBlockCipher engine = new AesEngine();

        // 创建 CTS 模式
        BufferedBlockCipher cipher = new CtsBlockCipher(new CbcBlockCipher(engine));

        // 初始化为加密模式
        cipher.Init(true, new ParametersWithIV(new KeyParameter(key), iv));

        // 加密
        byte[] output = new byte[cipher.GetOutputSize(plainBytes.Length)];
        int len = cipher.ProcessBytes(plainBytes, 0, plainBytes.Length, output, 0);
        len += cipher.DoFinal(output, len);

        // 如果输出长度小于缓冲区，裁剪结果
        if (len < output.Length)
        {
            byte[] result = new byte[len];
            Array.Copy(output, 0, result, 0, len);
            return result;
        }

        return output;
    }

    public static string Decrypt(byte[] cipherBytes, byte[] key, byte[] iv)
    {
        // 创建 AES 引擎
        IBlockCipher engine = new AesEngine();

        // 创建 CTS 模式
        BufferedBlockCipher cipher = new CtsBlockCipher(new CbcBlockCipher(engine));

        // 初始化为解密模式
        cipher.Init(false, new ParametersWithIV(new KeyParameter(key), iv));

        // 解密
        byte[] output = new byte[cipher.GetOutputSize(cipherBytes.Length)];
        int len = cipher.ProcessBytes(cipherBytes, 0, cipherBytes.Length, output, 0);
        len += cipher.DoFinal(output, len);

        // 如果输出长度小于缓冲区，裁剪结果
        if (len < output.Length)
        {
            byte[] result = new byte[len];
            Array.Copy(output, 0, result, 0, len);
            return Encoding.UTF8.GetString(result);
        }

        return Encoding.UTF8.GetString(output);
    }
}