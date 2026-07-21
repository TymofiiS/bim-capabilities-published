namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Deterministic in-memory registry of known capabilities.
/// </summary>
public sealed class CapabilityRegistry
{
    private readonly IReadOnlyDictionary<string, CapabilityDefinition> _capabilitiesByKey;

    public CapabilityRegistry(IReadOnlyList<CapabilityDefinition> definitions)
    {
        ArgumentGuard.ThrowIfNull(definitions);

        _capabilitiesByKey = definitions
            .GroupBy(definition => CreateKey(definition.EngineId, definition.CapabilityId))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        Definitions = definitions;
    }

    public IReadOnlyList<CapabilityDefinition> Definitions { get; }

    public bool TryGetDefinition(string engineId, string capabilityId, out CapabilityDefinition? definition)
    {
        if (string.IsNullOrWhiteSpace(engineId) || string.IsNullOrWhiteSpace(capabilityId))
        {
            definition = null;
            return false;
        }

        return _capabilitiesByKey.TryGetValue(CreateKey(engineId, capabilityId), out definition);
    }

    internal static string CreateKey(string engineId, string capabilityId)
    {
        return $"{engineId.Trim()}::{capabilityId.Trim()}";
    }
}
