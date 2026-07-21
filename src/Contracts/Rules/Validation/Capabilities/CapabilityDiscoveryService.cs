namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Discovers capabilities from the canonical capability registry.
/// </summary>
public sealed class CapabilityDiscoveryService : ICapabilityDiscoveryService
{
    private readonly CapabilityRegistry _registry;

    public CapabilityDiscoveryService()
        : this(BimRuleCapabilityRegistry.Default)
    {
    }

    public CapabilityDiscoveryService(CapabilityRegistry registry)
    {
        ArgumentGuard.ThrowIfNull(registry);
        _registry = registry;
    }

    public IReadOnlyList<CapabilityDefinition> GetCapabilities()
    {
        return _registry.Definitions
            .OrderBy(definition => definition.EngineId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(definition => definition.CapabilityId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public CapabilityDefinition? GetCapability(string engineId, string capabilityId)
    {
        return _registry.TryGetDefinition(engineId, capabilityId, out var definition)
            ? definition
            : null;
    }

    public IReadOnlyList<CapabilityDefinition> GetSupportedCapabilities()
    {
        return _registry.Definitions
            .Where(definition => definition.Status == CapabilityCompatibilityStatus.Supported)
            .OrderBy(definition => definition.EngineId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(definition => definition.CapabilityId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
