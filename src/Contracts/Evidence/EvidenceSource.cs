namespace BIMCapabilities.Contracts.Evidence;

/// <summary>
/// Identifies the engine and capability that produced evidence.
/// </summary>
public sealed record EvidenceSource
{
    public required string EngineId { get; init; }

    public string? AtomId { get; init; }

    public string? RuleId { get; init; }

    public string? CapabilityId { get; init; }
}
