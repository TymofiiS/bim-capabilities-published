using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Contracts.Engines.Naming.Pattern;

namespace BIMCapabilities.Contracts.Engines.Naming.Write;

/// <summary>
/// Input for converting naming compliance findings into write requests.
/// </summary>
public sealed record NamingWriteRequestBuildRequest
{
    public required NamingComplianceResult ComplianceResult { get; init; }

    public required NamingTargetSet TargetSet { get; init; }

    public IReadOnlyList<string>? RequiredPrefixes { get; init; }

    public NamingPatternRule? PatternRule { get; init; }

    public IReadOnlyList<NamingWriteCorrectionIntent>? CorrectionIntents { get; init; }

    public PrefixFixScope PrefixFixScope { get; init; }

    public DateTimeOffset RequestedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
