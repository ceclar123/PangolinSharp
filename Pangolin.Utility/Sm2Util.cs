using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Pangolin.Utility;

public static class Sm2Util
{
    /// <summary>
    /// SM2 是一种基于椭圆曲线密码学的非对称加密算法，常用于数字签名和密钥交换
    /// </summary>
    /// <returns></returns>
    public static AsymmetricCipherKeyPair GenerateSm2KeyPair()
    {
        // 获取 SM2 椭圆曲线参数
        X9ECParameters ecParams = GMNamedCurves.GetByName("sm2p256v1");
        ECDomainParameters domainParams = new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H);

        // 初始化密钥生成器
        ECKeyPairGenerator keyGen = new ECKeyPairGenerator("EC");
        // 使用 "EC" 作为算法名称
        ECKeyGenerationParameters keyGenParams = new ECKeyGenerationParameters(domainParams, new SecureRandom());
        keyGen.Init(keyGenParams);

        // 生成密钥对
        return keyGen.GenerateKeyPair();
    }
}