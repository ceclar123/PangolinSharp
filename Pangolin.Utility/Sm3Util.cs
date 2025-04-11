using Org.BouncyCastle.Crypto.Digests;

namespace Pangolin.Utility;

public static class Sm3Util
{
    /// <summary>
    ///  SM3 是一种密码哈希算法，类似于 SHA-256，但具有更高的安全性
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static byte[] CalculateHash(byte[] data)
    {
        // 创建SM3摘要对象
        SM3Digest sm3Digest = new SM3Digest();
        // 更新数据
        sm3Digest.BlockUpdate(data, 0, data.Length);
        // 获取摘要结果
        byte[] hash = new byte[sm3Digest.GetDigestSize()];
        sm3Digest.DoFinal(hash, 0);

        return hash;
    }
}