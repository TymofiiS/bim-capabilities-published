using BIMCapabilities.Contracts.Diagnostics;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

namespace BIMCapabilities.Composition.Capabilities.Handlers;

internal abstract class CapabilityHandlerSupport
{
    protected static IReadOnlyList<string> ParseCommaSeparatedValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return StringParsing.SplitCommaSeparated(value);
    }

    protected static string? GetConfigurationValue(
        IReadOnlyDictionary<string, string> configuration,
        string key)
    {
        return configuration.TryGetValue(key, out var value) ? value : null;
    }
}
