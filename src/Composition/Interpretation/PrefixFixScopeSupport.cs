using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Composition.Interpretation;

internal static class PrefixFixScopeSupport
{
    internal static PrefixFixScope Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PrefixFixScope.None;
        }

        var scope = PrefixFixScope.None;
        foreach (var token in StringParsing.SplitCommaSeparated(value))
        {
            if (token.Equals("both", StringComparison.OrdinalIgnoreCase)
                || token.Equals("all", StringComparison.OrdinalIgnoreCase)
                || token.Equals("type,family", StringComparison.OrdinalIgnoreCase)
                || token.Equals("family,type", StringComparison.OrdinalIgnoreCase))
            {
                return PrefixFixScope.All;
            }

            if (token.Equals("type", StringComparison.OrdinalIgnoreCase))
            {
                scope |= PrefixFixScope.Type;
            }
            else if (token.Equals("family", StringComparison.OrdinalIgnoreCase))
            {
                scope |= PrefixFixScope.Family;
            }
        }

        return scope;
    }
}
