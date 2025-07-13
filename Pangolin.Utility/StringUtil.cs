namespace Pangolin.Utility;

public static class StringUtil
{
    public static string Trim(string? input)
    {
        if (input is null)
        {
            return string.Empty;
        }

        return input.Trim();
    }
}