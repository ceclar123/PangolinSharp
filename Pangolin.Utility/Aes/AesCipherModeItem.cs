using System.Security.Cryptography;

namespace Pangolin.Utility.Aes;

[Serializable]
public class AesCipherModeItem
{
    public string? CfgName { get; set; }
    public CipherMode Mode { get; set; }
}