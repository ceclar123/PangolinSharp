using System.Security.Cryptography;

namespace Pangolin.Utility.Des;

[Serializable]
public class DesPaddingModeItem
{
    public string? CfgName { get; set; }

    public PaddingMode Padding { get; set; }
}