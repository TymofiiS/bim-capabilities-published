namespace BIMCapabilities.Contracts.Reports.Aggregation;

/// <summary>
/// Describes how evidence should be grouped for reporting.
/// </summary>
public sealed record EvidenceAggregationRule
{
    public required string RuleId { get; init; }

    public required string Name { get; init; }

    public string? GroupBy { get; init; }

    public string? Strategy { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
