using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Engines.Family.Filtering;

/// <summary>
/// Aggregate statistics for a Family Engine filtering atom operation.
/// </summary>
public sealed record FamilyFilterStatistics
{
    public int CandidateFamilies { get; init; }

    public int FilteredFamilies { get; init; }

    public int RemovedFamilies { get; init; }

    public IReadOnlyDictionary<string, int>? CountsByCategory { get; init; }
}

/// <summary>
/// Result of a Family Engine filtering atom operation.
/// </summary>
public sealed record FamilyFilterResult
{
    public required string AtomId { get; init; }

    public required IReadOnlyList<NormalizedFamily> FilteredFamilies { get; init; }

    public FamilyFilterStatistics? Statistics { get; init; }

    public IReadOnlyList<FamilyEngineDiagnostic>? Diagnostics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
