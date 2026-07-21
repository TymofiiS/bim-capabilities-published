namespace BIMCapabilities.Contracts.Evidence;

/// <summary>
/// Aggregate container for evidence records produced during execution.
/// </summary>
public sealed record EvidenceCollection
{
    public required string CollectionId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyList<EvidenceRecord> Records { get; init; } = [];
}
