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

    public static bool IsBlank(string? input)
    {
        return string.IsNullOrWhiteSpace(input);
    }

    public static bool IsNotBlank(string? input)
    {
        return !string.IsNullOrWhiteSpace(input);
    }

    public static bool AllNotBlank(params string?[]? input)
    {
        if (input is null || input.Length == 0)
        {
            return false;
        }

        foreach (var item in input)
        {
            if (IsBlank(item))
            {
                return false;
            }
        }

        return true;
    }

    public static bool AnyNotBlank(params string?[]? input)
    {
        if (input is null || input.Length == 0)
        {
            return false;
        }

        foreach (var item in input)
        {
            if (IsNotBlank(item))
            {
                return true;
            }
        }

        return false;
    }
}