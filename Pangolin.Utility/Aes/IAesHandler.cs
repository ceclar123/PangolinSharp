using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Paddings;

namespace Pangolin.Utility.Aes;

public interface IAesHandler
{
    /// <summary>
    ///  加密
    /// </summary>
    /// <param name="plainTextData">明文</param>
    /// <param name="padding">padding</param>
    /// <param name="cipherParam">密钥信息</param>
    /// <returns></returns>
    byte[] Encrypt(byte[] plainTextData, IBlockCipherPadding padding, ICipherParameters cipherParam);

    /// <summary>
    /// 解密
    /// </summary>
    /// <param name="cipherTextData">密文</param>
    /// <param name="padding">padding</param>
    /// <param name="cipherParam">密钥信息</param>
    /// <returns></returns>
    byte[] Decrypt(byte[] cipherTextData, IBlockCipherPadding padding, ICipherParameters cipherParam);
}