using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace Pangolin.Utility;

public static class Sm4Util
{
    /// <summary>
    /// 加密
    /// </summary>
    /// <param name="plainText">明文</param>
    /// <param name="key">key</param>
    /// <param name="iv">iv</param>
    /// <returns></returns>
    public static byte[] Encrypt(byte[] plainText, byte[] key, byte[] iv)
    {
        var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new SM4Engine()), new Pkcs7Padding());
        cipher.Init(true, new ParametersWithIV(new KeyParameter(key), iv));
        return cipher.DoFinal(plainText);
    }

    /// <summary>
    /// 解密
    /// </summary>
    /// <param name="cipherText">密文</param>
    /// <param name="key">key</param>
    /// <param name="iv">iv</param>
    /// <returns></returns>
    public static byte[] Decrypt(byte[] cipherText, byte[] key, byte[] iv)
    {
        var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new SM4Engine()), new Pkcs7Padding());
        cipher.Init(false, new ParametersWithIV(new KeyParameter(key), iv));
        return cipher.DoFinal(cipherText);
    }
}