namespace BIMCapabilities.Contracts.Diagnostics;

public static class StringParsing
{
    public static string[] SplitCommaSeparated(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Array.Empty<string>();
        }

        return value.Split(',')
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToArray();
    }

    public static string[] SplitDelimited(string value, char delimiter)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Array.Empty<string>();
        }

        return value.Split(delimiter)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToArray();
    }
}
