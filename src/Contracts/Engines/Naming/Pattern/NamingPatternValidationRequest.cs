using BIMCapabilities.Contracts.Engines.Naming;

namespace BIMCapabilities.Contracts.Engines.Naming.Pattern;

/// <summary>
/// Input for the Naming Engine pattern validation atom.
/// </summary>
public sealed record NamingPatternValidationRequest
{
    public required NamingTargetSet TargetSet { get; init; }

    public required NamingPatternRule Rule { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
