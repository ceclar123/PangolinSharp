using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace Pangolin.Utility;

public static class AesUtil
{
    /// <summary>
    /// 加密
    /// </summary>
    /// <param name="plainTextData">明文</param>
    /// <param name="key">必须是16, 24, 或32字节长以匹配AES-128, AES-192, or AES-256</param>
    /// <param name="iv">必须是16字节长</param>
    /// <returns></returns>
    public static byte[] Encrypt(byte[] plainTextData, byte[] key, byte[] iv)
    {
        IBlockCipher symmetricBlockCipher = new AesEngine();
        IBlockCipherMode symmetricBlockMode = new CbcBlockCipher(symmetricBlockCipher);
        IBlockCipherPadding padding = new Pkcs7Padding();

        PaddedBufferedBlockCipher cbcCipher = new PaddedBufferedBlockCipher(symmetricBlockMode, padding);

        cbcCipher.Init(true, new ParametersWithIV(new KeyParameter(key), iv));
        int blockSize = cbcCipher.GetBlockSize();
        byte[] cipherTextData = new byte[cbcCipher.GetOutputSize(plainTextData.Length)];
        int processLength = cbcCipher.ProcessBytes(plainTextData, 0, plainTextData.Length, cipherTextData, 0);
        int finalLength = cbcCipher.DoFinal(cipherTextData, processLength);
        byte[] finalCipherTextData = new byte[cipherTextData.Length - (blockSize - finalLength)];
        Array.Copy(cipherTextData, 0, finalCipherTextData, 0, finalCipherTextData.Length);
        return finalCipherTextData;
    }

    /// <summary>
    /// 解密
    /// </summary>
    /// <param name="cipherTextData">密文</param>
    /// <param name="key">必须是16, 24, 或32字节长以匹配AES-128, AES-192, or AES-256</param>
    /// <param name="iv">必须是16字节长</param>
    /// <returns></returns>
    public static byte[] Decrypt(byte[] cipherTextData, byte[] key, byte[] iv)
    {
        IBlockCipher symmetricBlockCipher = new AesEngine();
        IBlockCipherMode symmetricBlockMode = new CbcBlockCipher(symmetricBlockCipher);
        IBlockCipherPadding padding = new Pkcs7Padding();

        PaddedBufferedBlockCipher cbcCipher = new PaddedBufferedBlockCipher(symmetricBlockMode, padding);

        cbcCipher.Init(false, new ParametersWithIV(new KeyParameter(key), iv));
        int blockSize = cbcCipher.GetBlockSize();
        byte[] plainTextData = new byte[cbcCipher.GetOutputSize(cipherTextData.Length)];
        int processLength = cbcCipher.ProcessBytes(cipherTextData, 0, cipherTextData.Length, plainTextData, 0);
        int finalLength = cbcCipher.DoFinal(plainTextData, processLength);
        byte[] finalPlainTextData = new byte[plainTextData.Length - (blockSize - finalLength)];
        Array.Copy(plainTextData, 0, finalPlainTextData, 0, finalPlainTextData.Length);
        return finalPlainTextData;
    }
}