namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Defines a capability known to the current BIMCapabilities implementation.
/// </summary>
public sealed record CapabilityDefinition
{
    public required string CapabilityId { get; init; }

    public required string DisplayName { get; init; }

    public required string Description { get; init; }

    public required string EngineId { get; init; }

    public CapabilityCompatibilityStatus Status { get; init; } = CapabilityCompatibilityStatus.Supported;

    public CapabilityConfigurationSchema ConfigurationSchema { get; init; } = new();

    public IReadOnlyList<CapabilityExample> Examples { get; init; } = [];

    /// <summary>
    /// Binds this capability to an execution handler registered in the capability platform.
    /// </summary>
    public required string HandlerId { get; init; }

    /// <summary>
    /// Optional engine implementation atom identifier used for documentation synchronization.
    /// </summary>
    public string? ImplementationAtomId { get; init; }

    /// <summary>
    /// Supported replacement capability for deprecated entries.
    /// </summary>
    public string? ReplacementCapabilityId { get; init; }
}
