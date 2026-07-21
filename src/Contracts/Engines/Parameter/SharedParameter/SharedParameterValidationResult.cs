using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;

/// <summary>
/// Shared parameter validation outcome classification.
/// </summary>
public enum SharedParameterValidationStatus
{
    Valid,

    Missing,

    NotShared,

    DefinitionMismatch
}

/// <summary>
/// Shared parameter validation outcome for a single object and parameter.
/// </summary>
public sealed record SharedParameterValidationFinding
{
    public required string ObjectId { get; init; }

    public string? ObjectKind { get; init; }

    public string? ObjectName { get; init; }

    public required string ParameterName { get; init; }

    public required SharedParameterValidationStatus Status { get; init; }

    public required bool Passed { get; init; }

    public SharedParameterDefinition? ExpectedDefinition { get; init; }

    public NormalizedParameter? RetrievedParameter { get; init; }
}

/// <summary>
/// Aggregate statistics for a shared parameter validation operation.
/// </summary>
public sealed record SharedParameterValidationStatistics
{
    public int ObjectsChecked { get; init; }

    public int ObjectsPassed { get; init; }

    public int ObjectsFailed { get; init; }

    public int SharedParametersChecked { get; init; }

    public int MissingSharedParameters { get; init; }

    public int InvalidSharedParameters { get; init; }
}

/// <summary>
/// Result of a Parameter Engine shared parameter validation operation.
/// </summary>
public sealed record SharedParameterValidationResult
{
    public required string AtomId { get; init; }

    public IReadOnlyList<SharedParameterDefinition>? LoadedDefinitions { get; init; }

    public IReadOnlyList<SharedParameterValidationFinding>? Findings { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public IReadOnlyList<global::BIMCapabilities.Contracts.Engines.Parameter.ParameterEngineDiagnostic>? Diagnostics { get; init; }

    public SharedParameterValidationStatistics? Statistics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
