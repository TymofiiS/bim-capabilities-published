namespace BIMCapabilities.Contracts.Engines.Family;

/// <summary>
/// Aggregate statistics for a Family Engine selection operation.
/// </summary>
public sealed record FamilySelectionStatistics
{
    public int CandidateFamilies { get; init; }

    public int SelectedFamilies { get; init; }

    public int FilteredFamilies { get; init; }

    public IReadOnlyDictionary<string, int>? CountsByCategory { get; init; }
}

/// <summary>
/// Result of a Family Engine selection operation.
/// </summary>
public sealed record FamilySelectionResult
{
    public required FamilyTargetSet SelectedFamilies { get; init; }

    public IReadOnlyList<FamilyEngineDiagnostic>? Diagnostics { get; init; }

    public FamilySelectionStatistics? Statistics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
