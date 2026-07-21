namespace BIMCapabilities.Contracts.Engines.Registration;

/// <summary>
/// Describes a capability published by an engine.
/// </summary>
public sealed record EngineCapability
{
    public required string CapabilityName { get; init; }

    public string? CapabilityVersion { get; init; }

    public string? Description { get; init; }

    public string? CapabilityCategory { get; init; }
}
