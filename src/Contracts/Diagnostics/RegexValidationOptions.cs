using System.Text.RegularExpressions;

namespace BIMCapabilities.Contracts.Diagnostics;

public static class RegexValidationOptions
{
    public const RegexOptions Default =
#if REVIT2024
        RegexOptions.CultureInvariant | RegexOptions.Compiled;
#else
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking;
#endif
}
