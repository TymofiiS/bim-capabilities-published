namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Example configuration for a capability.
/// </summary>
public sealed record CapabilityExample
{
    public required string Description { get; init; }

    public IReadOnlyDictionary<string, string> Configuration { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
