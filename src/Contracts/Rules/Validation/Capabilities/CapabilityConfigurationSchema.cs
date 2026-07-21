namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Describes the configuration keys accepted by a capability.
/// </summary>
public sealed record CapabilityConfigurationSchema
{
    public IReadOnlyList<CapabilityConfigurationKey> Keys { get; init; } = [];
}
