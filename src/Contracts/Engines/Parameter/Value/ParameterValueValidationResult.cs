using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Parameter.Value;

/// <summary>
/// Parameter value validation outcome classification.
/// </summary>
public enum ParameterValueValidationStatus
{
    Valid,

    MissingValue,

    InvalidValue
}

/// <summary>
/// Parameter value validation outcome for a single object and parameter.
/// </summary>
public sealed record ParameterValueValidationFinding
{
    public required string ObjectId { get; init; }

    public string? ObjectKind { get; init; }

    public string? ObjectName { get; init; }

    public required string ParameterName { get; init; }

    public required ParameterValueValidationStatus Status { get; init; }

    public required bool Passed { get; init; }

    public string? ActualValue { get; init; }

    public ParameterValueRule? Rule { get; init; }

    public string? ViolationReason { get; init; }
}

/// <summary>
/// Aggregate statistics for a parameter value validation operation.
/// </summary>
public sealed record ParameterValueValidationStatistics
{
    public int ObjectsChecked { get; init; }

    public int ObjectsPassed { get; init; }

    public int ObjectsFailed { get; init; }

    public int ParametersChecked { get; init; }

    public int InvalidValues { get; init; }

    public int MissingValues { get; init; }
}

/// <summary>
/// Result of a Parameter Engine parameter value validation operation.
/// </summary>
public sealed record ParameterValueValidationResult
{
    public required string AtomId { get; init; }

    public IReadOnlyList<ParameterValueValidationFinding>? Findings { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public IReadOnlyList<global::BIMCapabilities.Contracts.Engines.Parameter.ParameterEngineDiagnostic>? Diagnostics { get; init; }

    public ParameterValueValidationStatistics? Statistics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
