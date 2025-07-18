using System.Security.Cryptography;
using System.Text;

namespace Pangolin.Utility.Des;

public static class DesOfbUtil
{
    public static byte[] Encrypt(byte[] plaintext, byte[] key, byte[] iv, PaddingMode paddingMode)
    {
        return ProcessOfb(plaintext, key, iv, paddingMode);
    }

    public static string Decrypt(byte[] ciphertext, byte[] key, byte[] iv, PaddingMode paddingMode)
    {
        // OFB模式下，解密过程与加密过程相同
        return Encoding.UTF8.GetString(ProcessOfb(ciphertext, key, iv, paddingMode));
    }

    private static byte[] ProcessOfb(byte[] inputBytes, byte[] key, byte[] iv, PaddingMode paddingMode)
    {
        byte[] outputBytes = new byte[inputBytes.Length];
        byte[] feedbackRegister = (byte[])iv.Clone();
        byte[] encryptedBlock = new byte[8];

        // 创建DES加密器对象，只用于加密，不用于解密
        using (DES des = DES.Create())
        {
            des.Key = key;
            des.Mode = CipherMode.ECB;
            des.Padding = paddingMode;

            using (ICryptoTransform encryptor = des.CreateEncryptor())
            {
                int blockCount = (inputBytes.Length + 7) / 8;

                for (int i = 0; i < blockCount; i++)
                {
                    // 加密反馈寄存器
                    encryptor.TransformBlock(feedbackRegister, 0, 8, encryptedBlock, 0);

                    // 计算当前块的长度（最后一块可能不足8字节）
                    int currentBlockSize = Math.Min(8, inputBytes.Length - i * 8);

                    // 使用加密后的反馈寄存器与输入数据进行XOR操作
                    for (int j = 0; j < currentBlockSize; j++)
                    {
                        int bytePos = i * 8 + j;
                        outputBytes[bytePos] = (byte)(inputBytes[bytePos] ^ encryptedBlock[j]);
                    }

                    // 更新反馈寄存器
                    Array.Copy(encryptedBlock, 0, feedbackRegister, 0, 8);
                }
            }
        }

        return outputBytes;
    }
}