using System.Security.Cryptography;

namespace Pangolin.Utility.TripleDes;

[Serializable]
public class TripleDesCipherModeItem
{
    public string? CfgName { get; set; }
    public CipherMode Mode { get; set; }
}