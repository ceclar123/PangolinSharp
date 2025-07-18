using System.Security.Cryptography;
using System.Text;
using Pangolin.Utility;

namespace PangolinUnitTest;

public class RsaUtilTest
{
    private RSA rsa;
    private string publicKey;
    private string privateKey;


    [SetUp]
    public void Setup()
    {
        rsa = RsaUtil.GenerateRsaKey(4096);
        RsaUtil.ExportKeys(rsa, out publicKey, out privateKey);
    }

    [TearDown]
    public void TearDown()
    {
        rsa.Clear();
        rsa.Dispose();
    }

    [Test]
    public void Test1()
    {
        string input = "Hello, *&*测试数据*&* RSA Encryption!";
        byte[] dataToEncrypt = Encoding.UTF8.GetBytes(input);

        // PKCS#1 v1.5 填充模式加密
        byte[] pkcs1Encrypted = RsaUtil.Encrypt(dataToEncrypt, rsa, RSAEncryptionPadding.Pkcs1);
        byte[] pkcs1Decrypted = RsaUtil.Decrypt(pkcs1Encrypted, rsa, RSAEncryptionPadding.Pkcs1);
        Assert.True(input.Equals(Encoding.UTF8.GetString(pkcs1Decrypted)));

        // OAEP 填充模式 (更安全)
        byte[] oaepSha1Encrypted = RsaUtil.Encrypt(dataToEncrypt, rsa, RSAEncryptionPadding.OaepSHA1);
        byte[] oaepSha1Decrypted = RsaUtil.Decrypt(oaepSha1Encrypted, rsa, RSAEncryptionPadding.OaepSHA1);
        Assert.True(input.Equals(Encoding.UTF8.GetString(oaepSha1Decrypted)));

        // OAEP 使用 SHA-256 哈希
        byte[] oaepSha256Encrypted = RsaUtil.Encrypt(dataToEncrypt, rsa, RSAEncryptionPadding.OaepSHA256);
        byte[] oaepSha256Decrypted = RsaUtil.Decrypt(oaepSha256Encrypted, rsa, RSAEncryptionPadding.OaepSHA256);
        Assert.True(input.Equals(Encoding.UTF8.GetString(oaepSha256Decrypted)));

        // OAEP 使用 SHA-384 哈希
        byte[] oaepSha384Encrypted = RsaUtil.Encrypt(dataToEncrypt, rsa, RSAEncryptionPadding.OaepSHA384);
        byte[] oaepSha384Decrypted = RsaUtil.Decrypt(oaepSha384Encrypted, rsa, RSAEncryptionPadding.OaepSHA384);
        Assert.True(input.Equals(Encoding.UTF8.GetString(oaepSha384Decrypted)));

        // OAEP 使用 SHA-512 哈希
        byte[] oaepSha512Encrypted = RsaUtil.Encrypt(dataToEncrypt, rsa, RSAEncryptionPadding.OaepSHA512);
        byte[] oaepSha512Decrypted = RsaUtil.Decrypt(oaepSha512Encrypted, rsa, RSAEncryptionPadding.OaepSHA512);
        Assert.True(input.Equals(Encoding.UTF8.GetString(oaepSha512Decrypted)));

        // // OAEP 使用 SHA3-256 哈希
        // byte[] oaepSha3_256Encrypted = RsaUtil.Encrypt(dataToEncrypt, rsa, RSAEncryptionPadding.OaepSHA3_256);
        // byte[] oaepSha3_256Decrypted = RsaUtil.Decrypt(oaepSha3_256Encrypted, rsa, RSAEncryptionPadding.OaepSHA3_256);
        // Assert.True(input.Equals(Encoding.UTF8.GetString(oaepSha3_256Decrypted)));
        //
        // // OAEP 使用 SHA3-384 哈希
        // byte[] oaepSha3_384Encrypted = RsaUtil.Encrypt(dataToEncrypt, rsa, RSAEncryptionPadding.OaepSHA3_384);
        // byte[] oaepSha3_384Decrypted = RsaUtil.Decrypt(oaepSha3_384Encrypted, rsa, RSAEncryptionPadding.OaepSHA3_384);
        // Assert.True(input.Equals(Encoding.UTF8.GetString(oaepSha3_384Decrypted)));
        //
        // // OAEP 使用 SHA3-512 哈希
        // byte[] oaepSha3_512Encrypted = RsaUtil.Encrypt(dataToEncrypt, rsa, RSAEncryptionPadding.OaepSHA3_512);
        // byte[] oaepSha3_512Decrypted = RsaUtil.Decrypt(oaepSha3_512Encrypted, rsa, RSAEncryptionPadding.OaepSHA3_512);
        // Assert.True(input.Equals(Encoding.UTF8.GetString(oaepSha3_512Decrypted)));
    }

    [Test]
    public void Test2()
    {
        string input = "Hello, *&*测试数据*&* RSA Encryption!";
        byte[] dataToSign = Encoding.UTF8.GetBytes(input);

        // PKCS#1 v1.5 签名模式 + SHA-256
        byte[] pkcs1Signature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        bool pkcs1Verified = RsaUtil.VerifyData(dataToSign, pkcs1Signature, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Assert.IsTrue(pkcs1Verified);

        // PSS 签名模式 + SHA-256 (更安全)
        byte[] pssSignature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        bool pssVerified = RsaUtil.VerifyData(dataToSign, pssSignature, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        Assert.IsTrue(pssVerified);

        // 其他哈希算法示例
        // SHA-1 (不推荐用于新系统)
        byte[] sha1Signature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        bool sha1Verified = RsaUtil.VerifyData(dataToSign, sha1Signature, rsa, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        Assert.IsTrue(sha1Verified);

        byte[] sha1PssSignature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.SHA1, RSASignaturePadding.Pss);
        bool sha1PassVerified = RsaUtil.VerifyData(dataToSign, sha1PssSignature, rsa, HashAlgorithmName.SHA1, RSASignaturePadding.Pss);
        Assert.IsTrue(sha1PassVerified);

        // SHA-384
        byte[] sha384Pkcs1Signature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1);
        bool sha354Pkcs1Verified = RsaUtil.VerifyData(dataToSign, sha384Pkcs1Signature, rsa, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1);
        Assert.IsTrue(sha354Pkcs1Verified);

        byte[] sha384Signature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.SHA384, RSASignaturePadding.Pss);
        bool sha354Verified = RsaUtil.VerifyData(dataToSign, sha384Signature, rsa, HashAlgorithmName.SHA384, RSASignaturePadding.Pss);
        Assert.IsTrue(sha354Verified);

        // SHA-512
        byte[] sha512Pkcs1Signature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        bool sha512Pkcs1Verified = RsaUtil.VerifyData(dataToSign, sha512Pkcs1Signature, rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        Assert.IsTrue(sha512Pkcs1Verified);

        byte[] sha512Signature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pss);
        bool sha512Verified = RsaUtil.VerifyData(dataToSign, sha512Signature, rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pss);
        Assert.IsTrue(sha512Verified);

        // MD5
        byte[] md5Pkcs1Signature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.MD5, RSASignaturePadding.Pkcs1);
        bool md5Pkcs1Verified = RsaUtil.VerifyData(dataToSign, md5Pkcs1Signature, rsa, HashAlgorithmName.MD5, RSASignaturePadding.Pkcs1);
        Assert.IsTrue(md5Pkcs1Verified);

        byte[] md5PssSignature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.MD5, RSASignaturePadding.Pss);
        bool md5PassVerified = RsaUtil.VerifyData(dataToSign, md5PssSignature, rsa, HashAlgorithmName.MD5, RSASignaturePadding.Pss);
        Assert.IsTrue(md5PassVerified);

        // SHA3_256 暂时不支持
        // byte[] sha3256Pkcs1Signature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.SHA3_256, RSASignaturePadding.Pkcs1);
        // bool sha3256Pkcs1Verified = RsaUtil.VerifyData(dataToSign, sha3256Pkcs1Signature, rsa, HashAlgorithmName.SHA3_256, RSASignaturePadding.Pkcs1);
        // Assert.IsTrue(sha3256Pkcs1Verified);
        //
        // byte[] sha3256PssSignature = RsaUtil.SignData(dataToSign, rsa, HashAlgorithmName.SHA3_256, RSASignaturePadding.Pss);
        // bool sha3256PssVerified = RsaUtil.VerifyData(dataToSign, sha3256PssSignature, rsa, HashAlgorithmName.SHA3_256, RSASignaturePadding.Pss);
        // Assert.IsTrue(sha3256PssVerified);
    }
}