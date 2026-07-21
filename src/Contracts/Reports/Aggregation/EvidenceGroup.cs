namespace BIMCapabilities.Contracts.Reports.Aggregation;

/// <summary>
/// A grouped set of evidence references prepared for reporting.
/// </summary>
public sealed record EvidenceGroup
{
    public required string GroupKey { get; init; }

    public required string GroupName { get; init; }

    public IReadOnlyList<string> EvidenceReferences { get; init; } = [];

    public EvidenceSummary? Summary { get; init; }
}
