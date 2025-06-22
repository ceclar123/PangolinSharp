using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace Pangolin.Utility;

public class AesModeItem
{
    public string CfgName { get; set; }
    public IBlockCipherMode Mode { get; set; }
    public bool HasIv { get; set; }

    public AesModeItem()
    {
    }

    public AesModeItem(string cfgName, IBlockCipherMode mode, bool hasIv)
    {
        CfgName = cfgName;
        Mode = mode;
        HasIv = hasIv;
    }
}

[Serializable]
public class AesPaddingItem
{
    public string CfgName { get; set; }

    public IBlockCipherPadding Padding { get; set; }

    public AesPaddingItem()
    {
    }


    public AesPaddingItem(string cfgName, IBlockCipherPadding padding)
    {
        CfgName = cfgName;
        Padding = padding;
    }
}

public static class AesUtil
{
    private static readonly IBlockCipher AesBlockCipher = new AesEngine();

    public static List<AesModeItem> BlockCipherModeList =
    [
        new AesModeItem(nameof(CbcBlockCipher), new CbcBlockCipher(AesBlockCipher), true),
        new AesModeItem(nameof(CfbBlockCipher), new CfbBlockCipher(AesBlockCipher, 8), true),
        new AesModeItem(nameof(EcbBlockCipher), new EcbBlockCipher(AesBlockCipher), false),
        // BouncyCastle 中的 GCTR（Galois Counter Mode 的缩写，尽管在 BouncyCastle 中它主要作为通用计数器模式用于其他目的）被设计用于处理 64 位块大小的加密算法。这意味着它适用于如 DES、TripleDES 等这类具有 64 位块大小的加密算法，而不直接适用于 AES 这样的 128 位块大小的加密算法
        // new AesModeItem(nameof(GOfbBlockCipher), new GOfbBlockCipher(AesBlockCipher), true),
        new AesModeItem(nameof(KCtrBlockCipher), new KCtrBlockCipher(AesBlockCipher), true),
        new AesModeItem(nameof(OfbBlockCipher), new OfbBlockCipher(AesBlockCipher, 64), true),
        new AesModeItem(nameof(OpenPgpCfbBlockCipher), new OpenPgpCfbBlockCipher(AesBlockCipher), true),
        new AesModeItem(nameof(SicBlockCipher), new SicBlockCipher(AesBlockCipher), true)
    ];

    public static List<AesPaddingItem> BlockCipherPaddingList =
    [
        new AesPaddingItem(nameof(Pkcs7Padding), new Pkcs7Padding()),
        new AesPaddingItem(nameof(ISO7816d4Padding), new ISO7816d4Padding()),
        new AesPaddingItem(nameof(ISO10126d2Padding), new ISO10126d2Padding()),
        new AesPaddingItem(nameof(TbcPadding), new TbcPadding()),
        new AesPaddingItem(nameof(X923Padding), new X923Padding()),
        new AesPaddingItem(nameof(ZeroBytePadding), new ZeroBytePadding())
    ];


    public static ICipherParameters GetCipherParam(byte[] key)
    {
        return new KeyParameter(key);
    }

    public static ICipherParameters GetCipherParam(byte[] key, byte[] iv)
    {
        return new ParametersWithIV(new KeyParameter(key), iv);
    }

    public static byte[] Encrypt(byte[] plainTextData, IBlockCipherMode symmetricBlockMode, IBlockCipherPadding padding, ICipherParameters keyParamWithIv)
    {
        // IBlockCipher symmetricBlockCipher = new AesEngine();
        // IBlockCipherMode symmetricBlockMode = new CbcBlockCipher(symmetricBlockCipher);
        // IBlockCipherPadding padding = new Pkcs7Padding();

        PaddedBufferedBlockCipher cbcCipher = new PaddedBufferedBlockCipher(symmetricBlockMode, padding);
        cbcCipher.Init(true, keyParamWithIv);
        int blockSize = cbcCipher.GetBlockSize();
        byte[] cipherTextData = new byte[cbcCipher.GetOutputSize(plainTextData.Length)];
        int processLength = cbcCipher.ProcessBytes(plainTextData, 0, plainTextData.Length, cipherTextData, 0);
        int finalLength = cbcCipher.DoFinal(cipherTextData, processLength);
        byte[] finalCipherTextData = new byte[cipherTextData.Length - (blockSize - finalLength)];
        Array.Copy(cipherTextData, 0, finalCipherTextData, 0, finalCipherTextData.Length);
        return finalCipherTextData;
    }

    public static byte[] Decrypt(byte[] cipherTextData, IBlockCipherMode symmetricBlockMode, IBlockCipherPadding padding, ICipherParameters keyParamWithIv)
    {
        // IBlockCipher symmetricBlockCipher = new AesEngine();        
        // IBlockCipherMode symmetricBlockMode = new CbcBlockCipher(symmetricBlockCipher);
        // IBlockCipherPadding padding  = new Pkcs7Padding();

        PaddedBufferedBlockCipher cbcCipher = new PaddedBufferedBlockCipher(symmetricBlockMode, padding);
        cbcCipher.Init(false, keyParamWithIv);
        int blockSize = cbcCipher.GetBlockSize();
        byte[] plainTextData = new byte[cbcCipher.GetOutputSize(cipherTextData.Length)];
        int processLength = cbcCipher.ProcessBytes(cipherTextData, 0, cipherTextData.Length, plainTextData, 0);
        int finalLength = cbcCipher.DoFinal(plainTextData, processLength);
        byte[] finalPlainTextData = new byte[plainTextData.Length - (blockSize - finalLength)];
        Array.Copy(plainTextData, 0, finalPlainTextData, 0, finalPlainTextData.Length);
        return finalPlainTextData;
    }


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