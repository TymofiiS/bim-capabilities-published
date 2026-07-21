using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Engines.Family.Selection;

/// <summary>
/// Aggregate statistics for a Family Engine selection atom operation.
/// </summary>
public sealed record FamilySelectionStatistics
{
    public int CandidateFamilies { get; init; }

    public int SelectedFamilies { get; init; }

    public int RejectedFamilies { get; init; }

    public IReadOnlyDictionary<string, int>? CountsByCategory { get; init; }
}

/// <summary>
/// Result of a Family Engine selection atom operation.
/// </summary>
public sealed record FamilySelectionResult
{
    public required string AtomId { get; init; }

    public required IReadOnlyList<NormalizedFamily> SelectedFamilies { get; init; }

    public FamilySelectionStatistics? Statistics { get; init; }

    public IReadOnlyList<FamilyEngineDiagnostic>? Diagnostics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
