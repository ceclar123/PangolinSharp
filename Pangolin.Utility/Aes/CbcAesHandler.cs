using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;

namespace Pangolin.Utility.Aes;

/// <summary>
///  CBC 模式是块密码的一种工作模式，它通过将前一个密文块与当前明文块进行异或操作来加密数据，从而增加了加密的安全性
///  1、密钥长度128位、192位、256位
///  2、初始向量16字节
/// 需要填充：由于它是基于块的加密方式，因此输入数据长度如果不是块大小的整数倍，则需要填充
/// 错误传播：如果一个块被篡改了，那么该块及其后续的所有块都无法正确解密
/// 初始向量（IV）：加密和解密时都需要使用相同的 IV，通常是一个随机生成的值
/// </summary>
///
///
public class CbcAesHandler : IAesHandler
{
    public const string Name = "CbcBlockCipher";

    public byte[] Encrypt(byte[] plainTextData, IBlockCipherPadding padding, ICipherParameters cipherParam)
    {
        IBlockCipher symmetricBlockCipher = new AesEngine();
        IBlockCipherMode symmetricBlockMode = new CbcBlockCipher(symmetricBlockCipher);
        // IBlockCipherPadding padding  = new Pkcs7Padding();

        PaddedBufferedBlockCipher cbcCipher = new PaddedBufferedBlockCipher(symmetricBlockMode, padding);
        cbcCipher.Init(true, cipherParam);
        int blockSize = cbcCipher.GetBlockSize();
        byte[] cipherTextData = new byte[cbcCipher.GetOutputSize(plainTextData.Length)];
        int processLength = cbcCipher.ProcessBytes(plainTextData, 0, plainTextData.Length, cipherTextData, 0);
        int finalLength = cbcCipher.DoFinal(cipherTextData, processLength);
        byte[] finalCipherTextData = new byte[cipherTextData.Length - (blockSize - finalLength)];
        Array.Copy(cipherTextData, 0, finalCipherTextData, 0, finalCipherTextData.Length);
        return finalCipherTextData;
    }

    public byte[] Decrypt(byte[] cipherTextData, IBlockCipherPadding padding, ICipherParameters cipherParam)
    {
        IBlockCipher symmetricBlockCipher = new AesEngine();
        IBlockCipherMode symmetricBlockMode = new CbcBlockCipher(symmetricBlockCipher);
        // IBlockCipherPadding padding  = new Pkcs7Padding();

        PaddedBufferedBlockCipher cbcCipher = new PaddedBufferedBlockCipher(symmetricBlockMode, padding);
        cbcCipher.Init(false, cipherParam);
        int blockSize = cbcCipher.GetBlockSize();
        byte[] plainTextData = new byte[cbcCipher.GetOutputSize(cipherTextData.Length)];
        int processLength = cbcCipher.ProcessBytes(cipherTextData, 0, cipherTextData.Length, plainTextData, 0);
        int finalLength = cbcCipher.DoFinal(plainTextData, processLength);
        byte[] finalPlainTextData = new byte[plainTextData.Length - (blockSize - finalLength)];
        Array.Copy(plainTextData, 0, finalPlainTextData, 0, finalPlainTextData.Length);
        return finalPlainTextData;
    }
}