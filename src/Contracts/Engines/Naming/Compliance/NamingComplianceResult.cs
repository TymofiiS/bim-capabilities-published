using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Engines.Naming.Prefix;
using BIMCapabilities.Contracts.Engines.Naming.Pattern;

namespace BIMCapabilities.Contracts.Engines.Naming.Compliance;

/// <summary>
/// Unified compliance finding produced by the Naming Engine composition workflow.
/// </summary>
public sealed record NamingComplianceFinding
{
    public required string ValidationStage { get; init; }

    public required string ObjectId { get; init; }

    public string? ObjectKind { get; init; }

    public string? ObjectName { get; init; }

    public required bool Passed { get; init; }

    public string? Status { get; init; }

    public string? Message { get; init; }
}

/// <summary>
/// Aggregate statistics for a Naming Engine compliance operation.
/// </summary>
public sealed record NamingComplianceStatistics
{
    public int ObjectsChecked { get; init; }

    public int ObjectsPassed { get; init; }

    public int ObjectsFailed { get; init; }

    public int PrefixChecksRun { get; init; }

    public int PatternChecksRun { get; init; }

    public int MissingPrefixCount { get; init; }

    public int InvalidPrefixCount { get; init; }

    public int PatternViolations { get; init; }

    public int InvalidCharacterViolations { get; init; }

    public int LengthViolations { get; init; }
}

/// <summary>
/// High-level compliance summary for a Naming Engine operation.
/// </summary>
public sealed record NamingComplianceSummary
{
    public int ObjectsChecked { get; init; }

    public int PassedChecks { get; init; }

    public int FailedChecks { get; init; }

    public decimal CompliancePercentage { get; init; }

    public int NamingViolations { get; init; }
}

/// <summary>
/// Result of a Naming Engine compliance composition operation.
/// </summary>
public sealed record NamingComplianceResult
{
    public required string EngineId { get; init; }

    public PrefixValidationResult? PrefixResult { get; init; }

    public NamingPatternValidationResult? PatternResult { get; init; }

    public IReadOnlyList<NamingComplianceFinding>? Findings { get; init; }

    public IReadOnlyList<EvidenceRecord>? Evidence { get; init; }

    public IReadOnlyList<NamingEngineDiagnostic>? Diagnostics { get; init; }

    public NamingComplianceStatistics? Statistics { get; init; }

    public NamingComplianceSummary? Summary { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
