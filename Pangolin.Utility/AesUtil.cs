using System.Security.Cryptography;
using System.Text;
using Pangolin.Utility.Aes;

namespace Pangolin.Utility;

/**
 * <summary>
CipherMode	PKCS7	Zeros	ANSIX923	ISO10126	None
CBC	        ✅	    ✅	    ✅	        ✅	        ⚠️
ECB	        ✅	    ✅	    ✅	        ✅	        ⚠️
CFB	        ✅	    ✅	    ✅	        ✅	        ✅
OFB	        ✅	    ✅	    ✅	        ✅	        ✅
CTS	        ❌	    ❌	    ❌	        ❌	        ✅
GCM	        ❌	    ❌	    ❌	        ❌	        ✅
✅ - 完全兼容
⚠️ - 条件兼容（仅当输入数据长度是块大小的整数倍时才可用）
❌ - 不兼容，会抛出异常

1、IV的重要性：除ECB外，其他模式都需要初始化向量(IV)。IV不需要保密，但每次加密应该使用不同的IV
2、ECB的安全性问题：ECB模式不推荐用于加密大块数据，因为它不能很好地隐藏数据模式
3、填充模式的选择：PKCS7是最常用的填充方式，提供了良好的安全性
4、密钥管理：AES密钥长度应为16字节(128位)、24字节(192位)或32字节(256位)
5、异常处理：解密过程中，如果密文、密钥或IV不正确，会抛出异常
 </summary>
<remarks>
1、CBC 和 ECB 模式使用 None 填充仅当明文长度是块大小的倍数时才有效
2、CTS 模式专门设计为处理最后一个不完整块，因此它只能与 None 填充一起使用
3、CFB 和 OFB 是流加密模式，因此可以使用任何填充方式，包括 None
</remarks>
<remarks>
1、CBC (Cipher Block Chaining)
    - 每个明文块先与前一个密文块进行异或，然后再进行加密
    - 需要IV来处理第一个块
    - 更安全，但不能并行处理
    - 适用场景：需要高安全性的场合，是最常用的模式
2、ECB (Electronic Codebook)
    - 每个块独立加密，相同的明文块产生相同的密文块
    - 不使用IV，但.NET的API仍需要提供IV
    - 可并行化处理，简单
    - 安全性较低，不推荐用于敏感数据
    - 适用场景：加密非常短的数据（小于块大小）
3、CFB (Cipher Feedback)
    - 将前一个密文块加密，再与当前明文块异或
    - 将加密算法转换为流密码，可以处理任意长度的数据，无需填充
    - 错误传播，一个位错误会影响多个密文块
    - 适用场景：流数据加密，如网络通信
4、OFB (Output Feedback)
    - 类似于CFB，但使用加密器的输出而非密文进行反馈
    - 将加密算法转换为流密码
    - 错误不会传播，适合噪声通道
    - 对明文修改不敏感
    - 适用场景：卫星通信等高噪声环境
5、CTS (Ciphertext Stealing)
    - 一种处理最后一个块的特殊模式
6、GCM (Galois/Counter Mode)
    - 结合了CTR模式和认证功能
    - 通过 AesGcm 类单独实现，不是 CipherMode 的一部分
</remarks>
<remarks>
1、PKCS7：填充字节值等于填充的字节数
2、Zeros：用零字节填充
3、ANSIX923：除最后一个字节外，其余填充字节为零，最后一个字节表示填充的字节数
4、ISO10126：除最后一个字节外，其余填充字节为随机值，最后一个字节表示填充的字节数
5、None：不进行填充（要求数据长度是块大小的整数倍）
</remarks>
 */
public static class AesUtil
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

    public static List<AesCipherModeItem> GetAesCipherModeItemList()
    {
        return ValidCipherModePaddingModeDictionary.Keys
            .Select(it => new AesCipherModeItem()
            {
                Mode = it,
                CfgName = it.ToString()
            }).ToList();
    }

    public static List<AesPaddingModeItem> GetAesPaddingModeItemList(CipherMode cipherMode)
    {
        return ValidCipherModePaddingModeDictionary.TryGetValue(cipherMode, out var list)
            ? list.Select(it => new AesPaddingModeItem()
            {
                Padding = it,
                CfgName = it.ToString()
            }).ToList()
            : new List<AesPaddingModeItem>();
    }

    /// <summary>
    /// 检查CipherMode和PaddingMode的组合是否有效
    /// </summary>
    /// <param name="cipherMode">加密模式</param>
    /// <param name="paddingMode">填充方式</param>
    /// <param name="inputLength">输入数据长度</param>
    /// <param name="blockSize">块大小(字节)</param>
    /// <returns>如果组合有效则返回true，否则返回false</returns>
    public static bool IsValidCombination(CipherMode cipherMode, PaddingMode paddingMode, int inputLength = -1, int blockSize = 16)
    {
        // 检查模式是否在有效组合列表中
        if (!ValidCipherModePaddingModeDictionary.ContainsKey(cipherMode))
        {
            return false;
        }

        // 如果是特殊情况：CBC或ECB与None填充
        if ((cipherMode == CipherMode.CBC || cipherMode == CipherMode.ECB) && paddingMode == PaddingMode.None)
        {
            // 仅当输入长度是块大小的倍数时有效
            return inputLength > 0 && inputLength % blockSize == 0;
        }

        // 检查该模式是否支持指定的填充方式
        return ValidCipherModePaddingModeDictionary[cipherMode].Contains(paddingMode);
    }

    /// <summary>
    /// AES 加密
    /// </summary>
    /// <param name="plainText">待加密的明文</param>
    /// <param name="key">密钥</param>
    /// <param name="iv">初始化向量</param>
    /// <param name="cipherMode">加密模式</param>
    /// <param name="paddingMode">填充方式</param>
    /// <returns>加密后的密文（Base64编码）</returns>
    public static string Encrypt(string plainText, byte[] key, byte[] iv, CipherMode cipherMode, PaddingMode paddingMode)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentNullException(nameof(plainText), "明文不能为空");
        }

        if (key == null || key.Length == 0)
        {
            throw new ArgumentNullException(nameof(key), "密钥不能为空");
        }

        if (iv == null || iv.Length == 0)
        {
            throw new ArgumentNullException(nameof(iv), "初始化向量不能为空");
        }

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        // 检查模式和填充组合是否有效
        if (!IsValidCombination(cipherMode, paddingMode, plainBytes.Length))
        {
            throw new CryptographicException($"无效的加密模式和填充组合: {cipherMode} + {paddingMode}");
        }

        if (cipherMode == CipherMode.OFB)
        {
            return Convert.ToBase64String(AesOfbUtil.Encrypt(plainBytes, key, iv, paddingMode));
        }
        else if (cipherMode == CipherMode.CTS)
        {
            // CTS 只支持PaddingMode.None
            return Convert.ToBase64String(AesCtsUtil.Encrypt(plainBytes, key, iv));
        }

        try
        {
            byte[] cipherBytes;
            using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = cipherMode;
                aes.Padding = paddingMode;

                // ECB模式不使用IV
                if (cipherMode == CipherMode.ECB)
                {
                    // ECB模式不依赖IV，但.NET的API仍需要提供IV，这里提示用户
                    Console.WriteLine("注意: ECB模式不使用IV，即使提供了IV也会被忽略。");
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(plainBytes, 0, plainBytes.Length);
                            cs.FlushFinalBlock();
                        }
                    }

                    cipherBytes = ms.ToArray();
                }
            }

            return Convert.ToBase64String(cipherBytes);
        }
        catch (Exception ex)
        {
            throw new CryptographicException($"AES加密失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// AES 解密
    /// </summary>
    /// <param name="cipherText">待解密的密文（Base64编码）</param>
    /// <param name="key">密钥</param>
    /// <param name="iv">初始化向量</param>
    /// <param name="cipherMode">加密模式</param>
    /// <param name="paddingMode">填充方式</param>
    /// <returns>解密后的明文</returns>
    public static string Decrypt(string cipherText, byte[] key, byte[] iv, CipherMode cipherMode, PaddingMode paddingMode)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            throw new ArgumentNullException(nameof(cipherText), "密文不能为空");
        }

        if (key == null || key.Length == 0)
        {
            throw new ArgumentNullException(nameof(key), "密钥不能为空");
        }

        if (iv == null || iv.Length == 0)
        {
            throw new ArgumentNullException(nameof(iv), "初始化向量不能为空");
        }

        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            // 检查模式和填充组合是否有效
            if (!IsValidCombination(cipherMode, paddingMode, cipherBytes.Length))
            {
                throw new CryptographicException($"无效的加密模式和填充组合: {cipherMode} + {paddingMode}");
            }

            if (cipherMode == CipherMode.OFB)
            {
                return AesOfbUtil.Decrypt(cipherBytes, key, iv, paddingMode);
            }
            else if (cipherMode == CipherMode.CTS)
            {
                // CTS 只支持PaddingMode.None
                return AesCtsUtil.Decrypt(cipherBytes, key, iv);
            }

            byte[] plainBytes;
            using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = cipherMode;
                aes.Padding = paddingMode;

                // ECB模式不使用IV
                if (cipherMode == CipherMode.ECB)
                {
                    Console.WriteLine("注意: ECB模式不使用IV，即使提供了IV也会被忽略。");
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.FlushFinalBlock();
                        }
                    }

                    plainBytes = ms.ToArray();
                }
            }

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            throw new CryptographicException($"AES解密失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 生成随机密钥
    /// </summary>
    /// <param name="keySizeInBytes">密钥长度（16/24/32字节）</param>
    /// <returns>随机生成的密钥</returns>
    public static byte[] GenerateRandomKey(int keySizeInBytes)
    {
        ValidateKeySize(keySizeInBytes);

        byte[] key = new byte[keySizeInBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }

        return key;
    }

    /// <summary>
    /// 生成随机IV
    /// </summary>
    /// <returns>随机生成的16字节IV</returns>
    public static byte[] GenerateRandomIv()
    {
        // AES块大小固定为16字节
        byte[] iv = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }

        return iv;
    }

    /// <summary>
    /// 验证密钥长度是否合法
    /// </summary>
    /// <param name="keySizeInBytes">密钥长度（字节）</param>
    private static void ValidateKeySize(int keySizeInBytes)
    {
        if (keySizeInBytes != 16 && keySizeInBytes != 24 && keySizeInBytes != 32)
        {
            throw new ArgumentException("密钥长度必须为16、24或32字节（对应AES-128、AES-192、AES-256）");
        }
    }
}