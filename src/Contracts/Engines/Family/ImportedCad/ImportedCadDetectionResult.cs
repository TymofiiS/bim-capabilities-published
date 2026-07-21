using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Family.ImportedCad;

/// <summary>
/// Imported CAD analysis outcome for a single family.
/// </summary>
public sealed record ImportedCadFinding
{
    public required NormalizedFamily Family { get; init; }

    public required bool HasImportedCad { get; init; }

    public IReadOnlyList<NormalizedRelationship>? ImportedCadRelationships { get; init; }

    public EvidenceSeverity? Severity { get; init; }
}

/// <summary>
/// Aggregate statistics for an imported CAD detection operation.
/// </summary>
public sealed record ImportedCadDetectionStatistics
{
    public int FamiliesChecked { get; init; }

    public int FamiliesPassed { get; init; }

    public int FamiliesFailed { get; init; }

    public int ImportedCadReferencesFound { get; init; }
}

/// <summary>
/// Result of the Family Engine imported CAD detection atom.
/// </summary>
public sealed record ImportedCadDetectionResult
{
    public required string AtomId { get; init; }

    public IReadOnlyList<NormalizedFamily>? AffectedFamilies { get; init; }

    public IReadOnlyList<ImportedCadFinding>? Findings { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public ImportedCadDetectionStatistics? Statistics { get; init; }

    public IReadOnlyList<FamilyEngineDiagnostic>? Diagnostics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
