using System.Security.Cryptography;

namespace Pangolin.Utility.TripleDes;

[Serializable]
public class TripleDesPaddingModeItem
{
    public string? CfgName { get; set; }

    public PaddingMode Padding { get; set; }
}