using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Parameter.Existence;
using BIMCapabilities.Contracts.Engines.Parameter.SharedParameter;
using BIMCapabilities.Contracts.Engines.Parameter.Value;

namespace BIMCapabilities.Contracts.Engines.Parameter.Compliance;

/// <summary>
/// Unified compliance finding produced by the Parameter Engine composition workflow.
/// </summary>
public sealed record ParameterComplianceFinding
{
    public required string ValidationStage { get; init; }

    public required string ObjectId { get; init; }

    public string? ObjectKind { get; init; }

    public string? ObjectName { get; init; }

    public required string ParameterName { get; init; }

    public required bool Passed { get; init; }

    public string? Status { get; init; }

    public string? Message { get; init; }
}

/// <summary>
/// Aggregate statistics for a Parameter Engine compliance operation.
/// </summary>
public sealed record ParameterComplianceStatistics
{
    public int ObjectsChecked { get; init; }

    public int ObjectsPassed { get; init; }

    public int ObjectsFailed { get; init; }

    public int ParametersChecked { get; init; }

    public int MissingParameters { get; init; }

    public int MissingSharedParameters { get; init; }

    public int InvalidSharedParameters { get; init; }

    public int MissingValues { get; init; }

    public int InvalidValues { get; init; }

    public int ExistenceChecksRun { get; init; }

    public int SharedParameterChecksRun { get; init; }

    public int ValueChecksRun { get; init; }
}

/// <summary>
/// High-level compliance summary for a Parameter Engine operation.
/// </summary>
public sealed record ParameterComplianceSummary
{
    public int ObjectsChecked { get; init; }

    public int ParametersChecked { get; init; }

    public int PassedChecks { get; init; }

    public int FailedChecks { get; init; }

    public decimal CompliancePercentage { get; init; }
}

/// <summary>
/// Result of a Parameter Engine compliance composition operation.
/// </summary>
public sealed record ParameterComplianceResult
{
    public required string EngineId { get; init; }

    public ParameterExistenceResult? ExistenceResult { get; init; }

    public SharedParameterValidationResult? SharedParameterResult { get; init; }

    public ParameterValueValidationResult? ValueResult { get; init; }

    public IReadOnlyList<ParameterComplianceFinding>? Findings { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public IReadOnlyList<global::BIMCapabilities.Contracts.Engines.Parameter.ParameterEngineDiagnostic>? Diagnostics { get; init; }

    public ParameterComplianceStatistics? Statistics { get; init; }

    public ParameterComplianceSummary? Summary { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
