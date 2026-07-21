namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Exposes registered capabilities for discovery and user-facing support queries.
/// </summary>
public interface ICapabilityDiscoveryService
{
    IReadOnlyList<CapabilityDefinition> GetCapabilities();

    CapabilityDefinition? GetCapability(string engineId, string capabilityId);

    IReadOnlyList<CapabilityDefinition> GetSupportedCapabilities();
}
