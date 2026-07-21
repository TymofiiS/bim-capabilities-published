namespace BIMCapabilities.Contracts.Reports.Aggregation;

/// <summary>
/// Quantitative evidence metrics used by summaries and groups.
/// </summary>
public sealed record EvidenceStatistics
{
    public int TotalCount { get; init; }

    public IReadOnlyDictionary<string, int>? Counts { get; init; }

    public IReadOnlyDictionary<string, int>? Totals { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>? Breakdowns { get; init; }

    public IReadOnlyDictionary<string, decimal>? Percentages { get; init; }
}
