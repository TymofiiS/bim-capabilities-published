using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Pattern;
using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Contracts.Engines.Naming.Compliance;

/// <summary>
/// Input for the Naming Engine compliance composition workflow.
/// </summary>
public sealed record NamingComplianceRequest
{
    public required NamingTargetSet TargetSet { get; init; }

    public IReadOnlyList<string>? RequiredPrefixes { get; init; }

    public bool CaseSensitive { get; init; }

    public NamingPatternRule? PatternRule { get; init; }

    /// <summary>
    /// When set, limits prefix validation to the same objects automatic correction may rename.
    /// None validates both family and type names.
    /// </summary>
    public PrefixFixScope PrefixFixScope { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
