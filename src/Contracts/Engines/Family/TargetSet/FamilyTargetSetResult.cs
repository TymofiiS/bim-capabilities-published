using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Family.TargetSet;

/// <summary>
/// Aggregate statistics for a Family Engine target set generation operation.
/// </summary>
public sealed record FamilyTargetSetStatistics
{
    public int DiscoveredFamilies { get; init; }

    public int SelectedFamilies { get; init; }

    public int FilteredFamilies { get; init; }

    public int ComplianceCheckedFamilies { get; init; }

    public int TargetFamilies { get; init; }

    public int ImportedCadReferencesFound { get; init; }
}

/// <summary>
/// Result of a Family Engine target set generation operation.
/// </summary>
public sealed record FamilyTargetSetResult
{
    public required string GeneratorId { get; init; }

    public required global::BIMCapabilities.Contracts.Engines.Family.FamilyTargetSet TargetSet { get; init; }

    public FamilyTargetSetStatistics? Statistics { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public IReadOnlyList<global::BIMCapabilities.Contracts.Engines.Family.FamilyEngineDiagnostic>? Diagnostics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
