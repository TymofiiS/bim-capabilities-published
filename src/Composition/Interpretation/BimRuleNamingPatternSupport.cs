using BIMCapabilities.Contracts.Engines.Naming.Pattern;

namespace BIMCapabilities.Composition.Interpretation;

internal static class BimRuleNamingPatternSupport
{
    internal static NamingPatternRule CreatePatternRule(string prefix)
    {
        ArgumentGuard.ThrowIfNullOrWhiteSpace(prefix);

        if (string.Equals(prefix, "DR_", StringComparison.Ordinal))
        {
            return CreateDoorPatternRule();
        }

        if (string.Equals(prefix, "WN_", StringComparison.Ordinal))
        {
            return CreateWindowPatternRule();
        }

        var escapedPrefix = System.Text.RegularExpressions.Regex.Escape(prefix);
        return new NamingPatternRule
        {
            TokenizedPattern = $"{prefix}{{Token}}",
            RegularExpression = $"^{escapedPrefix}[A-Za-z][A-Za-z0-9]*$",
            AllowedCharacters = "A-Za-z0-9_",
            ForbiddenCharacters = [" ", "-"]
        };
    }

    private static NamingPatternRule CreateDoorPatternRule()
    {
        return new NamingPatternRule
        {
            TokenizedPattern = "DR_{Token}",
            RegularExpression = @"^DR_[A-Za-z][A-Za-z0-9]*$",
            AllowedCharacters = "A-Za-z0-9_",
            ForbiddenCharacters = [" ", "-"]
        };
    }

    private static NamingPatternRule CreateWindowPatternRule()
    {
        return new NamingPatternRule
        {
            TokenizedPattern = "WN_{Token}",
            RegularExpression = @"^WN_[A-Za-z][A-Za-z0-9]*$",
            AllowedCharacters = "A-Za-z0-9_",
            ForbiddenCharacters = [" ", "-"]
        };
    }
}
