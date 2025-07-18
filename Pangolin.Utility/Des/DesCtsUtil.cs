using System.Security.Cryptography;
using System.Text;

namespace Pangolin.Utility.Des;

public static class DesCtsUtil
{
    private const int BlockSize = 8;

    public static byte[] Encrypt(byte[] plainBytes, byte[] key, byte[] iv)
    {
        if (plainBytes == null || plainBytes.Length == 0)
        {
            throw new ArgumentException("明文不能为空", nameof(plainBytes));
        }

        // 如果数据长度小于等于一个块，直接使用标准CBC模式
        if (plainBytes.Length <= BlockSize)
        {
            return EncryptWithStandardMode(plainBytes, key, iv);
        }

        // 计算需要处理的完整块的数量
        int fullBlocksCount = plainBytes.Length / BlockSize;
        int remainingBytes = plainBytes.Length % BlockSize;
        bool hasPartialBlock = remainingBytes > 0;

        // 如果数据长度恰好是块大小的整数倍，无需使用CTS
        if (!hasPartialBlock)
        {
            return EncryptWithStandardMode(plainBytes, key, iv);
        }

        // 实际需要处理的块数量
        int blocksCount = fullBlocksCount + (hasPartialBlock ? 1 : 0);

        // 创建结果数组，大小与输入相同（无填充）
        byte[] result = new byte[plainBytes.Length];

        using (DES des = DES.Create())
        {
            des.Key = key;
            des.Mode = CipherMode.ECB; // 使用ECB模式，手动实现CBC和CTS
            des.Padding = PaddingMode.None;

            using (ICryptoTransform encryptor = des.CreateEncryptor())
            {
                byte[] currentBlock = new byte[BlockSize];
                byte[] previousCipherBlock = (byte[])iv.Clone();
                byte[] cipherBlock = new byte[BlockSize];

                // 处理除最后两个块以外的所有块（标准CBC模式）
                for (int i = 0; i < fullBlocksCount - 1; i++)
                {
                    // 将当前块与前一个密文块进行XOR
                    for (int j = 0; j < BlockSize; j++)
                    {
                        currentBlock[j] = (byte)(plainBytes[i * BlockSize + j] ^ previousCipherBlock[j]);
                    }

                    // 加密
                    encryptor.TransformBlock(currentBlock, 0, BlockSize, cipherBlock, 0);

                    // 复制到结果数组
                    Array.Copy(cipherBlock, 0, result, i * BlockSize, BlockSize);

                    // 保存当前密文块为下一轮的前一个密文块
                    Array.Copy(cipherBlock, previousCipherBlock, BlockSize);
                }

                // 处理倒数第二个块（CTS处理的第一个特殊块）
                int secondLastBlockIndex = (fullBlocksCount - 1);
                int secondLastBlockOffset = secondLastBlockIndex * BlockSize;

                // 最后一个块（可能是部分块）
                int lastBlockOffset = fullBlocksCount * BlockSize;

                // 准备倒数第二个块
                for (int j = 0; j < BlockSize; j++)
                {
                    currentBlock[j] = (byte)(plainBytes[secondLastBlockOffset + j] ^ previousCipherBlock[j]);
                }

                // 加密倒数第二个块
                byte[] secondLastCipherBlock = new byte[BlockSize];
                encryptor.TransformBlock(currentBlock, 0, BlockSize, secondLastCipherBlock, 0);

                // 准备最后一个块（填充0）
                Array.Clear(currentBlock, 0, BlockSize);
                Array.Copy(plainBytes, lastBlockOffset, currentBlock, 0, remainingBytes);

                // 将最后一个块与倒数第二个密文块进行XOR
                for (int j = 0; j < BlockSize; j++)
                {
                    currentBlock[j] ^= secondLastCipherBlock[j];
                }

                // 加密最后一个块
                byte[] lastCipherBlock = new byte[BlockSize];
                encryptor.TransformBlock(currentBlock, 0, BlockSize, lastCipherBlock, 0);

                // CTS特殊处理：最后两个块的密文需要特殊处理
                // 1. 将最后一个密文块的前'remainingBytes'字节作为倒数第二个块的密文
                Array.Copy(lastCipherBlock, 0, result, secondLastBlockOffset, remainingBytes);

                // 2. 将倒数第二个密文块的尾部作为最后一个块的密文
                Array.Copy(secondLastCipherBlock, remainingBytes, result, lastBlockOffset, plainBytes.Length - lastBlockOffset);
            }
        }

        return result;
    }

    public static string Decrypt(byte[] cipherBytes, byte[] key, byte[] iv)
    {
        byte[] plainBytes = DecryptBytes(cipherBytes, key, iv);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public static byte[] DecryptBytes(byte[] cipherBytes, byte[] key, byte[] iv)
    {
        if (cipherBytes == null || cipherBytes.Length == 0)
        {
            throw new ArgumentException("密文不能为空", nameof(cipherBytes));
        }

        // 如果数据长度小于等于一个块，直接使用标准CBC模式
        if (cipherBytes.Length <= BlockSize)
        {
            return DecryptWithStandardMode(cipherBytes, key, iv);
        }

        // 计算需要处理的完整块的数量
        int fullBlocksCount = cipherBytes.Length / BlockSize;
        int remainingBytes = cipherBytes.Length % BlockSize;
        bool hasPartialBlock = remainingBytes > 0;

        // 如果数据长度恰好是块大小的整数倍，无需使用CTS
        if (!hasPartialBlock)
        {
            return DecryptWithStandardMode(cipherBytes, key, iv);
        }

        // 实际需要处理的块数量
        int blocksCount = fullBlocksCount + (hasPartialBlock ? 1 : 0);

        // 创建结果数组，大小与输入相同
        byte[] result = new byte[cipherBytes.Length];

        using (DES des = DES.Create())
        {
            des.Key = key;
            des.Mode = CipherMode.ECB; // 使用ECB模式，手动实现CBC和CTS
            des.Padding = PaddingMode.None;

            using (ICryptoTransform decryptor = des.CreateDecryptor())
            {
                byte[] currentBlock = new byte[BlockSize];
                byte[] previousCipherBlock = (byte[])iv.Clone();
                byte[] plainBlock = new byte[BlockSize];

                // 处理除最后两个块以外的所有块（标准CBC模式）
                for (int i = 0; i < fullBlocksCount - 1; i++)
                {
                    // 复制当前密文块
                    Array.Copy(cipherBytes, i * BlockSize, currentBlock, 0, BlockSize);
                    byte[] currentCipherBlock = (byte[])currentBlock.Clone();

                    // 解密
                    decryptor.TransformBlock(currentBlock, 0, BlockSize, plainBlock, 0);

                    // 与前一个密文块进行XOR
                    for (int j = 0; j < BlockSize; j++)
                    {
                        result[i * BlockSize + j] = (byte)(plainBlock[j] ^ previousCipherBlock[j]);
                    }

                    // 保存当前密文块为下一轮的前一个密文块
                    Array.Copy(currentCipherBlock, previousCipherBlock, BlockSize);
                }

                // 倒数第二个块和最后一个块的位置
                int secondLastBlockIndex = fullBlocksCount - 1;
                int secondLastBlockOffset = secondLastBlockIndex * BlockSize;
                int lastBlockOffset = fullBlocksCount * BlockSize;

                // 重建完整的倒数第二个密文块（C[n-1]）
                byte[] secondLastCipherBlock = new byte[BlockSize];
                // 前半部分来自倒数第二个接收到的密文块
                Array.Copy(cipherBytes, secondLastBlockOffset, secondLastCipherBlock, 0, remainingBytes);
                // 后半部分来自最后一个接收到的密文块
                Array.Copy(cipherBytes, lastBlockOffset, secondLastCipherBlock, remainingBytes, cipherBytes.Length - lastBlockOffset);

                // 解密倒数第二个密文块，得到P[n] ^ C[n-2]
                decryptor.TransformBlock(secondLastCipherBlock, 0, BlockSize, plainBlock, 0);

                // 得到最后一个明文块P[n]
                for (int j = 0; j < remainingBytes; j++)
                {
                    result[lastBlockOffset + j] = (byte)(plainBlock[j] ^ previousCipherBlock[j]);
                }

                // 重建最后一个完整的密文块（实际是C[n]的扩展版，用于解密）
                byte[] lastFullCipherBlock = new byte[BlockSize];
                // 使用C[n-1]的后半部分扩展C[n]
                Array.Copy(cipherBytes, secondLastBlockOffset, lastFullCipherBlock, 0, remainingBytes);
                // 填充0（实际上这部分内容不影响解密结果）
                for (int j = remainingBytes; j < BlockSize; j++)
                {
                    lastFullCipherBlock[j] = 0;
                }

                // 解密扩展的C[n]，得到P[n-1] ^ C[n-2]
                decryptor.TransformBlock(lastFullCipherBlock, 0, BlockSize, plainBlock, 0);

                // 得到倒数第二个明文块P[n-1]
                for (int j = 0; j < BlockSize; j++)
                {
                    result[secondLastBlockOffset + j] = (byte)(plainBlock[j] ^ previousCipherBlock[j]);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 使用标准模式（CBC）处理无需CTS的数据
    /// </summary>
    /// <param name="plainBytes">明文</param>
    /// <param name="key">密钥</param>
    /// <param name="iv">初始化向量</param>
    /// <returns></returns>
    private static byte[] EncryptWithStandardMode(byte[] plainBytes, byte[] key, byte[] iv)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (DES des = DES.Create())
            {
                des.Key = key;
                des.IV = iv;
                des.Mode = CipherMode.CBC;

                // 对于刚好是块大小整数倍的数据，使用ZeroPadding确保结果大小与输入相同
                des.Padding = plainBytes.Length % BlockSize == 0 ? PaddingMode.Zeros : PaddingMode.PKCS7;

                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                }

                byte[] result = ms.ToArray();

                // 如果使用了PKCS7填充，结果会比输入大，需要截断
                if (des.Padding == PaddingMode.PKCS7 && result.Length > plainBytes.Length)
                {
                    byte[] trimmedResult = new byte[plainBytes.Length];
                    Array.Copy(result, trimmedResult, plainBytes.Length);
                    return trimmedResult;
                }

                return result;
            }
        }
    }

    /// <summary>
    /// 使用标准模式（CBC）解密无需CTS的数据
    /// </summary>
    /// <param name="cipherBytes">密文</param>
    /// <param name="key">密钥</param>
    /// <param name="iv">初始化向量</param>
    /// <returns></returns>
    private static byte[] DecryptWithStandardMode(byte[] cipherBytes, byte[] key, byte[] iv)
    {
        // 创建临时数组，确保长度是块大小的整数倍
        int paddedLength = ((cipherBytes.Length + BlockSize - 1) / BlockSize) * BlockSize;
        byte[] paddedCipherBytes = new byte[paddedLength];
        Array.Copy(cipherBytes, paddedCipherBytes, cipherBytes.Length);

        using (DES des = DES.Create())
        {
            des.Key = key;
            des.IV = iv;
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.Zeros; // 使用零填充

            try
            {
                using (ICryptoTransform decryptor = des.CreateDecryptor())
                {
                    byte[] paddedPlainBytes = decryptor.TransformFinalBlock(paddedCipherBytes, 0, paddedLength);

                    // 截取到原始长度
                    byte[] result = new byte[cipherBytes.Length];
                    Array.Copy(paddedPlainBytes, result, cipherBytes.Length);
                    return result;
                }
            }
            catch
            {
                // 尝试使用PKCS7填充
                des.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform decryptor = des.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                }
            }
        }
    }
}