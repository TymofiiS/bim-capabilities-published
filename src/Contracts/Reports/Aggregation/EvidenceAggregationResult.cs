namespace BIMCapabilities.Contracts.Reports.Aggregation;

/// <summary>
/// Result of preparing evidence for report profiles.
/// </summary>
public sealed record EvidenceAggregationResult
{
    public required string AggregationId { get; init; }

    public required EvidenceSummary Summary { get; init; }

    public IReadOnlyList<EvidenceGroup> Groups { get; init; } = [];

    public EvidenceStatistics? Statistics { get; init; }
}
