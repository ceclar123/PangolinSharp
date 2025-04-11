namespace Pangolin.Utility;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System.Text;

public static class AesUtil
{
    /// <summary>
    /// 加密
    /// </summary>
    /// <param name="plaintext">明文</param>
    /// <param name="key">key</param>
    /// <param name="iv">iv</param>
    /// <returns></returns>
    public static byte[] Encrypt(string plaintext, byte[] key, byte[] iv)
    {

        IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CBC/PKCS7Padding");
        cipher.Init(true, new ParametersWithIV(new KeyParameter(key), iv));
        return cipher.DoFinal(Encoding.UTF8.GetBytes(plaintext));
    }

    /// <summary>
    /// 解密
    /// </summary>
    /// <param name="ciphertext">密文</param>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    public static string Decrypt(byte[] ciphertext, byte[] key, byte[] iv)
    {
        IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CBC/PKCS7Padding");
        cipher.Init(false, new ParametersWithIV(new KeyParameter(key), iv));
        byte[] plaintext = cipher.DoFinal(ciphertext);
        return Encoding.UTF8.GetString(plaintext);
    }
}