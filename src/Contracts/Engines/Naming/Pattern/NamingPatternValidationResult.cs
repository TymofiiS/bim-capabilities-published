using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Naming.Pattern;

/// <summary>
/// Naming pattern validation outcome classification.
/// </summary>
public enum NamingPatternValidationStatus
{
    Valid,

    EmptyName,

    PatternViolation,

    InvalidCharacter,

    ForbiddenCharacter,

    LengthViolation
}

/// <summary>
/// Naming pattern validation outcome for a single named object.
/// </summary>
public sealed record NamingPatternValidationFinding
{
    public required string ObjectId { get; init; }

    public string? ObjectKind { get; init; }

    public string? ObjectName { get; init; }

    public required NamingPatternValidationStatus Status { get; init; }

    public required bool Passed { get; init; }

    public string? ViolationReason { get; init; }

    public NamingPatternRule? Rule { get; init; }
}

/// <summary>
/// Aggregate statistics for a naming pattern validation operation.
/// </summary>
public sealed record NamingPatternValidationStatistics
{
    public int ObjectsChecked { get; init; }

    public int ObjectsPassed { get; init; }

    public int ObjectsFailed { get; init; }

    public int PatternViolations { get; init; }

    public int InvalidCharacterViolations { get; init; }

    public int LengthViolations { get; init; }
}

/// <summary>
/// Result of a Naming Engine pattern validation operation.
/// </summary>
public sealed record NamingPatternValidationResult
{
    public required string AtomId { get; init; }

    public IReadOnlyList<NamingPatternValidationFinding>? Findings { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public IReadOnlyList<NamingEngineDiagnostic>? Diagnostics { get; init; }

    public NamingPatternValidationStatistics? Statistics { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
