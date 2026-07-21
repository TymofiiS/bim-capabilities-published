namespace BIMCapabilities.Contracts.Engines.Family.Discovery;

/// <summary>
/// Input for Family Engine discovery atoms.
/// </summary>
public sealed record FamilyDiscoveryRequest
{
    public IReadOnlyList<string>? CategoryNames { get; init; }

    public IReadOnlyList<string>? FamilyNames { get; init; }

    public IReadOnlyList<string>? FamilyTypeNames { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
