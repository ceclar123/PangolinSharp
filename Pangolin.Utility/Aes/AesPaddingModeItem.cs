using System.Security.Cryptography;

namespace Pangolin.Utility.Aes;

[Serializable]
public class AesPaddingModeItem
{
    public string? CfgName { get; set; }

    public PaddingMode Padding { get; set; }
}