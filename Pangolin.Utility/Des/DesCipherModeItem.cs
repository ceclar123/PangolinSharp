using System.Security.Cryptography;

namespace Pangolin.Utility.Des;

[Serializable]
public class DesCipherModeItem
{
    public string? CfgName { get; set; }
    public CipherMode Mode { get; set; }
}