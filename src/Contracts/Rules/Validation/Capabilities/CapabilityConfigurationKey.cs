namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Describes one configuration key accepted by a capability.
/// </summary>
public sealed record CapabilityConfigurationKey
{
    public required string Key { get; init; }

    public required string Description { get; init; }

    public bool Required { get; init; }
}
