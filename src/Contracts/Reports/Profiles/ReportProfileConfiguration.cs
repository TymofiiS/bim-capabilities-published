namespace BIMCapabilities.Contracts.Reports.Profiles;

/// <summary>
/// Describes how a report profile selects and summarizes evidence.
/// </summary>
public sealed record ReportProfileConfiguration
{
    public string? EvidenceSelectionStrategy { get; init; }

    public string? AggregationStrategy { get; init; }

    public string? SummaryStrategy { get; init; }

    public string? PresentationIntent { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
