using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Pangolin.Utility;

public static class CommonUtil
{
    private const string HttpUrlPattern = @"^(http(s)?://)([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}(/[^/\s]*)*$";
    private const string DefaultChar = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static bool IsValidHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Regex.IsMatch(url ?? "", HttpUrlPattern, RegexOptions.IgnoreCase);
    }


    public static string Generate(int length)
    {
        byte[] data = new byte[length];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(data);
        }

        StringBuilder result = new StringBuilder(length);
        foreach (byte b in data)
        {
            result.Append(DefaultChar[b % DefaultChar.Length]);
        }

        return result.ToString();
    }
}