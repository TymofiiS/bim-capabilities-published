using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Parameter.Existence;

/// <summary>
/// Existence validation outcome for a single object and parameter.
/// </summary>
public sealed record ParameterExistenceFinding
{
    public required string ObjectId { get; init; }

    public string? ObjectKind { get; init; }

    public string? ObjectName { get; init; }

    public required string ParameterName { get; init; }

    public required bool Exists { get; init; }
}

/// <summary>
/// Aggregate statistics for a parameter existence validation operation.
/// </summary>
public sealed record ParameterExistenceStatistics
{
    public int ObjectsChecked { get; init; }

    public int ObjectsPassed { get; init; }

    public int ObjectsFailed { get; init; }

    public int MissingParameters { get; init; }
}

/// <summary>
/// Result of a Parameter Engine parameter existence validation operation.
/// </summary>
public sealed record ParameterExistenceResult
{
    public required string AtomId { get; init; }

    public IReadOnlyList<ParameterExistenceFinding>? Findings { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public IReadOnlyList<global::BIMCapabilities.Contracts.Engines.Parameter.ParameterEngineDiagnostic>? Diagnostics { get; init; }

    public ParameterExistenceStatistics? Statistics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
