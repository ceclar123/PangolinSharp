using System.Security.Cryptography;
using System.Text;
using Pangolin.Utility.Des;

namespace Pangolin.Utility;

/**
 * <summary>
CipherMode	PKCS7	Zeros	ANSIX923	ISO10126	None
ECB	       ✅	  ✅	     ✅	      ✅	     ⚠️
CBC	       ✅	  ✅	     ✅	      ✅	     ⚠️
CFB	       ✅	  ✅	     ✅	      ✅	     ✅
OFB	       ❌	  ❌	     ❌	      ❌	     ❌
CTS	       ❌	  ❌	     ❌	      ❌	     ❌
✅ = 支持
⚠️ = 有条件支持（数据长度必须是块大小的整数倍）
❌ = 不支持或会引发异常
KEY长度：16字节或24字节
IV长度：8字节
 * </summary>
 *
 */
public static class TripleDesUtil
{
    /// <summary>
    /// 存储有效的CipherMode和PaddingMode组合
    /// </summary>
    public static readonly Dictionary<CipherMode, List<PaddingMode>> ValidCipherModePaddingModeDictionary = new Dictionary<CipherMode, List<PaddingMode>>
    {
        {
            CipherMode.CBC,
            [PaddingMode.PKCS7, PaddingMode.Zeros, PaddingMode.ANSIX923, PaddingMode.ISO10126, PaddingMode.None]
        },
        {
            CipherMode.ECB,
            [PaddingMode.PKCS7, PaddingMode.Zeros, PaddingMode.ANSIX923, PaddingMode.ISO10126, PaddingMode.None]
        },
        {
            CipherMode.CFB,
            [PaddingMode.PKCS7, PaddingMode.Zeros, PaddingMode.ANSIX923, PaddingMode.ISO10126, PaddingMode.None]
        }
    };

    public static List<DesCipherModeItem> GetDesCipherModeItemList()
    {
        return ValidCipherModePaddingModeDictionary.Keys
            .Select(it => new DesCipherModeItem()
            {
                Mode = it,
                CfgName = it.ToString()
            }).ToList();
    }

    public static List<DesPaddingModeItem> GetDesPaddingModeItemList(CipherMode cipherMode)
    {
        return ValidCipherModePaddingModeDictionary.TryGetValue(cipherMode, out var list)
            ? list.Select(it => new DesPaddingModeItem()
            {
                Padding = it,
                CfgName = it.ToString()
            }).ToList()
            : new List<DesPaddingModeItem>();
    }


    public static string Encrypt(byte[] plainBytes, byte[] key, byte[] iv, CipherMode mode, PaddingMode padding)
    {
        try
        {
            // 对于NoPadding模式，确保数据长度是8的倍数
            if (padding == PaddingMode.None && plainBytes.Length % 8 != 0)
            {
                int paddingSize = 8 - (plainBytes.Length % 8);
                Array.Resize(ref plainBytes, plainBytes.Length + paddingSize);
            }

            byte[] cipherBytes;

            using (TripleDES tripleDes = TripleDES.Create())
            {
                tripleDes.Key = key;
                tripleDes.IV = iv;
                tripleDes.Mode = mode;
                tripleDes.Padding = padding;

                using (var encryptor = tripleDes.CreateEncryptor())
                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    cipherBytes = memoryStream.ToArray();
                }
            }

            return Convert.ToBase64String(cipherBytes);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("加密过程中发生错误: " + ex.Message, ex);
        }
    }


    public static string Decrypt(string cipherText, byte[] key, byte[] iv, CipherMode mode, PaddingMode padding)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] plainBytes;

            using (TripleDES tripleDes = TripleDES.Create())
            {
                tripleDes.Key = key;
                tripleDes.IV = iv;
                tripleDes.Mode = mode;
                tripleDes.Padding = padding;
                using (var decryptor = tripleDes.CreateDecryptor())
                using (var memoryStream = new MemoryStream(cipherBytes))
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (var resultStream = new MemoryStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        resultStream.Write(buffer, 0, bytesRead);
                    }

                    plainBytes = resultStream.ToArray();
                }
            }

            // 如果是NoPadding模式，去除可能的尾部零值
            if (padding == PaddingMode.None)
            {
                int lastNonZeroIndex = plainBytes.Length - 1;
                while (lastNonZeroIndex >= 0 && plainBytes[lastNonZeroIndex] == 0)
                {
                    lastNonZeroIndex--;
                }

                if (lastNonZeroIndex < plainBytes.Length - 1)
                {
                    Array.Resize(ref plainBytes, lastNonZeroIndex + 1);
                }
            }

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("解密过程中发生错误: " + ex.Message, ex);
        }
    }
}