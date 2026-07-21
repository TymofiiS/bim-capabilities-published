using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Reports.Aggregation;

/// <summary>
/// High-level summary of aggregated evidence.
/// </summary>
public sealed record EvidenceSummary
{
    public int TotalEvidence { get; init; }

    public IReadOnlyDictionary<EvidenceSeverity, int>? BySeverity { get; init; }

    public IReadOnlyDictionary<EvidenceCategory, int>? ByCategory { get; init; }

    public IReadOnlyDictionary<string, int>? BySource { get; init; }

    public IReadOnlyDictionary<string, int>? ByTarget { get; init; }

    public EvidenceStatistics? Statistics { get; init; }
}
