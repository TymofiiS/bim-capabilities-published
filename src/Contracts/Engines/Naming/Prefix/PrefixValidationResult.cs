using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Naming.Prefix;

/// <summary>
/// Prefix validation outcome classification.
/// </summary>
public enum PrefixValidationStatus
{
    Valid,

    EmptyName,

    MissingPrefix,

    InvalidPrefix
}

/// <summary>
/// Prefix validation outcome for a single named object.
/// </summary>
public sealed record PrefixValidationFinding
{
    public required string ObjectId { get; init; }

    public string? ObjectKind { get; init; }

    public string? ObjectName { get; init; }

    public required PrefixValidationStatus Status { get; init; }

    public required bool Passed { get; init; }

    public string? MatchedPrefix { get; init; }

    public IReadOnlyList<string>? RequiredPrefixes { get; init; }
}

/// <summary>
/// Aggregate statistics for a prefix validation operation.
/// </summary>
public sealed record PrefixValidationStatistics
{
    public int ObjectsChecked { get; init; }

    public int ObjectsPassed { get; init; }

    public int ObjectsFailed { get; init; }

    public int MissingPrefixCount { get; init; }

    public int InvalidPrefixCount { get; init; }
}

/// <summary>
/// Result of a Naming Engine prefix validation operation.
/// </summary>
public sealed record PrefixValidationResult
{
    public required string AtomId { get; init; }

    public IReadOnlyList<PrefixValidationFinding>? Findings { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public IReadOnlyList<NamingEngineDiagnostic>? Diagnostics { get; init; }

    public PrefixValidationStatistics? Statistics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
