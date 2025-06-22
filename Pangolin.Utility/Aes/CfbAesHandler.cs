using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;

namespace Pangolin.Utility.Aes;

/// <summary>
/// CFB 模式是一种块密码的工作模式，它将块密码转换为流密码使用。这种模式允许你对数据进行加密和解密，而不需要考虑数据的长度是否是块大小的整数倍
/// 不需要填充：因为它是基于流的加密方式
/// 错误传播有限：每个块只会影响下一个块
/// 支持前向同步：只要初始向量（IV）一致，就能正确解密
/// 不推荐用于需要高安全性认证的数据：因为它没有提供完整性检查或认证功能
/// </summary>
public class CfbAesHandler : IAesHandler
{
    public const string Name = "CfbBlockCipher";

    public byte[] Encrypt(byte[] plainTextData, IBlockCipherPadding padding, ICipherParameters cipherParam)
    {
        IBlockCipher symmetricBlockCipher = new AesEngine();

        // Next select the mode compatible with the "engine", in this case we 
        // use CFB mode as a streaming cipher - set the block size to 1 byte
        IBlockCipherMode symmetricBlockMode = new CfbBlockCipher(symmetricBlockCipher, symmetricBlockCipher.GetBlockSize() * 8);

        // apply the mode and engine on the cipherTextData
        StreamBlockCipher cfbCipher = new StreamBlockCipher(symmetricBlockMode);

        cfbCipher.Init(true, cipherParam);
        byte[] cipherTextData = new byte[plainTextData.Length];

        // simulate stream
        for (int j = 0; j < plainTextData.Length; j++)
        {
            cipherTextData[j] = cfbCipher.ReturnByte(plainTextData[j]);
        }

        return cipherTextData;
    }

    public byte[] Decrypt(byte[] cipherTextData, IBlockCipherPadding padding, ICipherParameters cipherParam)
    {
        // First choose the "engine", in this case IDEA 
        IBlockCipher symmetricBlockCipher = new IdeaEngine();

        // Next select the mode compatible with the "engine", in this case we 
        // use CFB mode as a streaming cipher - set the block size to 1 byte
        IBlockCipherMode symmetricBlockMode = new CfbBlockCipher(symmetricBlockCipher, 8);

        // apply the mode and engine on the cipherTextData
        StreamBlockCipher cfbCipher = new StreamBlockCipher(symmetricBlockMode);

        cfbCipher.Init(false, cipherParam);
        byte[] plainTextData = new byte[cipherTextData.Length];

        // simulate stream
        for (int j = 0; j < cipherTextData.Length; j++)
        {
            plainTextData[j] = cfbCipher.ReturnByte(cipherTextData[j]);
        }

        return plainTextData;
    }
}