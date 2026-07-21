namespace BIMCapabilities.Contracts.Reports.Aggregation;

/// <summary>
/// Describes an evidence aggregation request for report preparation.
/// </summary>
public sealed record EvidenceAggregation
{
    public required string AggregationId { get; init; }

    public required string SourceCollectionId { get; init; }

    public required EvidenceAggregationRule Rule { get; init; }

    public string? ProfileId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
