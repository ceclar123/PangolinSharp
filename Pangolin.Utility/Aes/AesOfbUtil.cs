using System.Security.Cryptography;
using System.Text;

namespace Pangolin.Utility.Aes;

public static class AesOfbUtil
{
    public static byte[] Encrypt(byte[] plainBytes, byte[] key, byte[] iv, PaddingMode paddingMode)
    {
        byte[] cipherBytes = new byte[plainBytes.Length];

        using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
        {
            aes.Key = key;
            // 使用ECB模式但自己实现OFB逻辑
            aes.Mode = CipherMode.ECB; 
            aes.Padding = paddingMode;

            byte[] ofbVector = new byte[iv.Length];
            Array.Copy(iv, ofbVector, iv.Length);

            ICryptoTransform encryptor = aes.CreateEncryptor();

            // 块大小（字节）
            int blockSize = aes.BlockSize / 8; 
            byte[] encryptedBlock = new byte[blockSize];

            for (int i = 0; i < plainBytes.Length; i += blockSize)
            {
                // 加密前一个输出块
                encryptor.TransformBlock(ofbVector, 0, blockSize, encryptedBlock, 0);

                // 更新OFB向量为加密结果
                Array.Copy(encryptedBlock, 0, ofbVector, 0, blockSize);

                // 计算当前处理的实际块大小
                int currentBlockSize = Math.Min(blockSize, plainBytes.Length - i);

                // 与明文进行XOR操作
                for (int j = 0; j < currentBlockSize; j++)
                {
                    cipherBytes[i + j] = (byte)(plainBytes[i + j] ^ encryptedBlock[j]);
                }
            }
        }

        return cipherBytes;
    }

    public static string Decrypt(byte[] cipherBytes, byte[] key, byte[] iv, PaddingMode paddingMode)
    {
        // OFB 模式下加密和解密操作相同
        byte[] plainBytes = new byte[cipherBytes.Length];

        using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = paddingMode;

            byte[] ofbVector = new byte[iv.Length];
            Array.Copy(iv, ofbVector, iv.Length);

            ICryptoTransform encryptor = aes.CreateEncryptor();

            int blockSize = aes.BlockSize / 8;
            byte[] encryptedBlock = new byte[blockSize];

            for (int i = 0; i < cipherBytes.Length; i += blockSize)
            {
                // 加密前一个输出块
                encryptor.TransformBlock(ofbVector, 0, blockSize, encryptedBlock, 0);

                // 更新OFB向量为加密结果
                Array.Copy(encryptedBlock, 0, ofbVector, 0, blockSize);

                // 计算当前处理的实际块大小
                int currentBlockSize = Math.Min(blockSize, cipherBytes.Length - i);

                // 与密文进行XOR操作得到明文
                for (int j = 0; j < currentBlockSize; j++)
                {
                    plainBytes[i + j] = (byte)(cipherBytes[i + j] ^ encryptedBlock[j]);
                }
            }
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}