using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Naming;

/// <summary>
/// Validation outcome for a single naming requirement.
/// </summary>
public sealed record NamingValidationFinding
{
    public required string ObjectId { get; init; }

    public string? ObjectKind { get; init; }

    public string? ObjectName { get; init; }

    public required bool Passed { get; init; }

    public string? Message { get; init; }

    public string? RuleIdentifier { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Aggregate statistics for a Naming Engine validation operation.
/// </summary>
public sealed record NamingValidationStatistics
{
    public int ObjectsChecked { get; init; }

    public int ObjectsPassed { get; init; }

    public int ObjectsFailed { get; init; }

    public int RulesChecked { get; init; }

    public int FindingsCount { get; init; }
}

/// <summary>
/// Result of a Naming Engine validation operation.
/// </summary>
public sealed record NamingValidationResult
{
    public IReadOnlyList<NamingValidationFinding>? Findings { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public IReadOnlyList<NamingEngineDiagnostic>? Diagnostics { get; init; }

    public NamingValidationStatistics? Statistics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
