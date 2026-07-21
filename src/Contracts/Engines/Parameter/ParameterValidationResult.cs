using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Parameter;

/// <summary>
/// Validation outcome for a single parameter requirement.
/// </summary>
public sealed record ParameterValidationFinding
{
    public required string ParameterName { get; init; }

    public required bool Passed { get; init; }

    public string? Message { get; init; }

    public string? TargetObjectId { get; init; }

    public string? TargetObjectKind { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Aggregate statistics for a Parameter Engine validation operation.
/// </summary>
public sealed record ParameterValidationStatistics
{
    public int ParametersChecked { get; init; }

    public int ParametersPassed { get; init; }

    public int ParametersFailed { get; init; }

    public int FindingsCount { get; init; }
}

/// <summary>
/// Result of a Parameter Engine validation operation.
/// </summary>
public sealed record ParameterValidationResult
{
    public IReadOnlyList<ParameterValidationFinding>? Findings { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public IReadOnlyList<ParameterEngineDiagnostic>? Diagnostics { get; init; }

    public ParameterValidationStatistics? Statistics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
