namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Default capability registry for the current BIMCapabilities implementation.
/// </summary>
public static class BimRuleCapabilityRegistry
{
    public static CapabilityRegistry Default { get; } = new(CapabilityCatalogDefinitions.All);
}
