using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Pangolin.Utility;

public static class RsaUtil
{
    /// <summary>
    /// 生成指定长度的RSA密钥对
    /// </summary>
    /// <param name="keySize">常用大小：1024, 2048, 3072, 4096</param>
    /// <returns></returns>
    public static RSA GenerateRsaKey(int keySize)
    {
        RSA rsa = RSA.Create();
        rsa.KeySize = keySize;
        return rsa;
    }

    public static void ExportKeys(RSA rsa, out string publicKey, out string privateKey)
    {
        // 导出公钥 (XML格式)
        publicKey = rsa.ToXmlString(false);

        // 导出私钥 (XML格式)
        privateKey = rsa.ToXmlString(true);

        // 或者使用PEM格式导出
        // publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        // privateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
    }

    #region PEM格式密钥导入导出

    /// <summary>
    /// 以PEM格式导出密钥
    /// </summary>
    /// <param name="rsa"></param>
    /// <returns></returns>
    public static string ExportPublicKeyToPem(RSA rsa)
    {
        byte[] publicKey = rsa.ExportSubjectPublicKeyInfo();
        string base64Key = Convert.ToBase64String(publicKey);

        return "-----BEGIN PUBLIC KEY-----\n" +
               string.Join("\n", Regex.Matches(base64Key, ".{1,64}")
                   .Cast<Match>()
                   .Select(m => m.Value)) +
               "\n-----END PUBLIC KEY-----";
    }

    /// <summary>
    /// 以PEM格式导出密钥
    /// </summary>
    /// <param name="rsa"></param>
    /// <returns></returns>
    public static string ExportPrivateKeyToPem(RSA rsa)
    {
        byte[] privateKey = rsa.ExportPkcs8PrivateKey();
        string base64Key = Convert.ToBase64String(privateKey);

        return "-----BEGIN PRIVATE KEY-----\n" +
               string.Join("\n", Regex.Matches(base64Key, ".{1,64}")
                   .Cast<Match>()
                   .Select(m => m.Value)) +
               "\n-----END PRIVATE KEY-----";
    }

    /// <summary>
    /// 从PEM格式导入密钥
    /// </summary>
    /// <param name="pemPublicKey"></param>
    /// <returns></returns>
    public static RSA ImportPublicKeyFromPem(string pemPublicKey)
    {
        string base64Key = pemPublicKey
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\n", "")
            .Trim();

        byte[] keyBytes = Convert.FromBase64String(base64Key);

        RSA rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
        return rsa;
    }

    public static RSA ImportPrivateKeyFromPem(string pemPrivateKey)
    {
        string base64Key = pemPrivateKey
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Trim();

        byte[] keyBytes = Convert.FromBase64String(base64Key);

        RSA rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(keyBytes, out _);
        return rsa;
    }

    #endregion

    # region 公钥加密-私钥解密

    /// <summary>
    /// 使用公钥加密
    /// </summary>
    /// <param name="data">明文数据</param>
    /// <param name="rsa">rsa</param>
    /// <param name="padding">填充规则</param>
    /// <returns></returns>
    public static byte[] Encrypt(byte[] data, RSA rsa, RSAEncryptionPadding padding)
    {
        return rsa.Encrypt(data, padding);
    }

    /// <summary>
    /// 使用私钥解密
    /// </summary>
    /// <param name="encryptedData">密文数据</param>
    /// <param name="rsa">rsa</param>
    /// <param name="padding">填充规则</param>
    /// <returns></returns>
    public static byte[] Decrypt(byte[] encryptedData, RSA rsa, RSAEncryptionPadding padding)
    {
        return rsa.Decrypt(encryptedData, padding);
    }

    #endregion

    #region 私钥签名-公钥验签

    /// <summary>
    /// 私钥签名
    /// </summary>
    /// <param name="data">待签名数据</param>
    /// <param name="rsa">rsa</param>
    /// <param name="hashAlgorithm">hash算法</param>
    /// <param name="padding">填充规则</param>
    /// <returns></returns>
    public static byte[] SignData(byte[] data, RSA rsa, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
    {
        return rsa.SignData(data, hashAlgorithm, padding);
    }

    /// <summary>
    /// 公钥验签
    /// </summary>
    /// <param name="data">待验签数据</param>
    /// <param name="signature">签名</param>
    /// <param name="rsa">rsa</param>
    /// <param name="hashAlgorithm">hash算法</param>
    /// <param name="padding">填充规则</param>
    /// <returns></returns>
    public static bool VerifyData(byte[] data, byte[] signature, RSA rsa, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
    {
        return rsa.VerifyData(data, signature, hashAlgorithm, padding);
    }

    #endregion

    #region 私钥HASH签名-公钥验签

    /// <summary>
    /// 私钥签名
    /// </summary>
    /// <param name="hash">数据hash值</param>
    /// <param name="rsa">rsa</param>
    /// <param name="hashAlgorithm">hash算法</param>
    /// <param name="padding">填充规则</param>
    /// <returns></returns>
    public static byte[] SignHash(byte[] hash, RSA rsa, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
    {
        return rsa.SignHash(hash, hashAlgorithm, padding);
    }

    /// <summary>
    /// 公钥验签
    /// </summary>
    /// <param name="hash">数据hash值</param>
    /// <param name="signature">签名</param>
    /// <param name="rsa">rsa</param>
    /// <param name="hashAlgorithm">hash算法</param>
    /// <param name="padding">填充规则</param>
    /// <returns></returns>
    public static bool VerifyHash(byte[] hash, byte[] signature, RSA rsa, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
    {
        return rsa.VerifyHash(hash, signature, hashAlgorithm, padding);
    }

    #endregion
}