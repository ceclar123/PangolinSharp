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
CTS	       ❌	  ❌	     ❌	      ❌	     ✅
✅ = 支持
⚠️ = 有条件支持（数据长度必须是块大小的整数倍）
❌ = 不支持或会引发异常
 * </summary>
 * <remarks>
1、ECB模式（Electronic Codebook）:
    - 最简单的模式，将明文分成固定大小的块，每块独立加密
    - 相同的明文块会产生相同的密文块，不够安全
    - 不需要初始向量IV
2、CBC模式（Cipher Block Chaining）:
    - 每个明文块先与前一个密文块进行XOR操作，然后加密
    - 第一个块使用初始向量IV
    - 更安全，常用模式
3、CFB模式（Cipher Feedback）:
    - 将前一个密文块加密，再与当前明文块XOR
    - 适用于流加密
    - 需要初始向量IV
4、OFB模式（Output Feedback）:
    - 类似CFB，但使用加密器的输出作为下一块的输入
    - 适用于流加密
    - 需要初始向量IV
    - 在.NET的System.Security.Cryptography.DES类并不实现这种模式
5、CTS模式（Ciphertext Stealing）:
    - CBC模式的变种，用于处理不完整的最后一个块
    - 不需要填充
 * </remarks>
 * <remarks>
1、PKCS7：最常用的填充方式，用相同字节填充，字节值等于填充的字节数
2、Zeros：使用0字节填充
3、ANSIX923：使用0填充，最后一个字节是填充字节的数量
4、ISO10126：除了最后一个字节外，使用随机字节填充，最后一个字节表示填充字节数
 * </remarks>
 */
public static class DesUtil
{
    /// <summary>
    /// 存储有效的CipherMode和PaddingMode组合
    /// </summary>
    public static readonly Dictionary<CipherMode, List<PaddingMode>> ValidCipherModePaddingModeDictionary = new Dictionary<CipherMode, List<PaddingMode>>
    {
        {
            CipherMode.CBC,
            [PaddingMode.PKCS7, PaddingMode.Zeros, PaddingMode.ANSIX923, PaddingMode.ISO10126]
        },
        {
            CipherMode.ECB,
            [PaddingMode.PKCS7, PaddingMode.Zeros, PaddingMode.ANSIX923, PaddingMode.ISO10126]
        },
        {
            CipherMode.CFB,
            [PaddingMode.PKCS7, PaddingMode.Zeros, PaddingMode.ANSIX923, PaddingMode.ISO10126, PaddingMode.None]
        },
        {
            CipherMode.OFB,
            [PaddingMode.PKCS7, PaddingMode.Zeros, PaddingMode.ANSIX923, PaddingMode.ISO10126, PaddingMode.None]
        },
        {
            CipherMode.CTS,
            [PaddingMode.None]
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

    /// <summary>
    /// DES加密
    /// </summary>
    /// <param name="plainText">明文</param>
    /// <param name="key">密钥（8字节）</param>
    /// <param name="iv">初始向量（8字节）</param>
    /// <param name="mode">加密模式</param>
    /// <param name="padding">填充方式</param>
    /// <returns>加密后的Base64字符串</returns>
    public static string Encrypt(string plainText, byte[] key, byte[] iv, CipherMode mode, PaddingMode padding)
    {
        ValidateParameters(mode, padding, plainText, key, iv);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        // 检查None填充模式下的数据长度
        if (padding == PaddingMode.None && mode != CipherMode.CFB)
        {
            if (plainBytes.Length % 8 != 0)
            {
                throw new ArgumentException("使用None填充模式时，数据长度必须是8的整数倍");
            }
        }

        if (mode == CipherMode.OFB)
        {
            return Convert.ToBase64String(DesOfbUtil.Encrypt(plainBytes, key, iv, padding));
        }
        else if (mode == CipherMode.CTS)
        {
            return Convert.ToBase64String(DesCtsUtil.Encrypt(plainBytes, key, iv));
        }

        byte[] cipherBytes;
        using (DES des = DES.Create())
        {
            des.Key = key;
            des.Mode = mode;
            des.Padding = padding;

            if (mode != CipherMode.ECB && iv != null)
            {
                des.IV = iv;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                    cipherBytes = ms.ToArray();
                }
            }
        }

        return Convert.ToBase64String(cipherBytes);
    }

    /// <summary>
    ///  DES解密
    /// </summary>
    /// <param name="cipherText">密文（Base64字符串</param>
    /// <param name="key">密钥（8字节）</param>
    /// <param name="iv">初始向量（8字节）</param>
    /// <param name="mode">加密模式</param>
    /// <param name="padding">填充方式</param>
    /// <returns>解密后的字符串</returns>
    /// <exception cref="ArgumentException"></exception>
    public static string Decrypt(string cipherText, byte[] key, byte[] iv, CipherMode mode, PaddingMode padding)
    {
        ValidateParameters(mode, padding, cipherText, key, iv);

        byte[] cipherBytes = Convert.FromBase64String(cipherText);

        // 检查None填充模式下的数据长度
        if (padding == PaddingMode.None && mode != CipherMode.CFB && mode != CipherMode.CTS)
        {
            if (cipherBytes.Length % 8 != 0)
            {
                throw new ArgumentException("使用None填充模式时，数据长度必须是8的整数倍");
            }
        }

        if (mode == CipherMode.OFB)
        {
            return DesOfbUtil.Decrypt(cipherBytes, key, iv, padding);
        }
        else if (mode == CipherMode.CTS)
        {
            return DesCtsUtil.Decrypt(cipherBytes, key, iv);
        }

        byte[] plainBytes;
        using (DES des = DES.Create())
        {
            des.Key = key;
            des.Mode = mode;
            des.Padding = padding;

            if (mode != CipherMode.ECB && iv != null)
            {
                des.IV = iv;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherBytes, 0, cipherBytes.Length);
                    cs.FlushFinalBlock();
                    plainBytes = ms.ToArray();
                }
            }
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// 校验加解密参数的合法性
    /// </summary>
    private static void ValidateParameters(CipherMode mode, PaddingMode padding, string text, byte[] key, byte[] iv)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentNullException(nameof(text), "文本不能为空");
        }

        if (key == null || key.Length != 8)
        {
            throw new ArgumentException("DES密钥长度必须为8字节", nameof(key));
        }

        // ECB模式不需要IV
        if (mode != CipherMode.ECB && (iv == null || iv.Length != 8))
        {
            throw new ArgumentException("DES初始向量必须为8字节", nameof(iv));
        }

        // CTS模式只能使用None填充
        if (mode == CipherMode.CTS && padding != PaddingMode.None)
        {
            throw new CryptographicException("CTS模式只能与None填充方式一起使用");
        }
    }

    /// <summary>
    /// 生成随机DES密钥（8字节）
    /// </summary>
    public static byte[] GenerateKey()
    {
        using (DES des = DES.Create())
        {
            des.GenerateKey();
            return des.Key;
        }
    }

    /// <summary>
    /// 生成随机DES初始向量（8字节）
    /// </summary>
    public static byte[] GenerateIV()
    {
        using (DES des = DES.Create())
        {
            des.GenerateIV();
            return des.IV;
        }
    }

    /// <summary>
    /// 检查CipherMode和PaddingMode组合是否兼容
    /// </summary>
    public static bool IsCompatible(CipherMode mode, PaddingMode padding)
    {
        // CTS模式只能与None填充一起使用
        if (mode == CipherMode.CTS && padding != PaddingMode.None)
        {
            return false;
        }

        return true;
    }
}