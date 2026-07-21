using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Contracts.Engines.Naming.Prefix;

/// <summary>
/// Input for the Naming Engine prefix validation atom.
/// </summary>
public sealed record PrefixValidationRequest
{
    public required NamingTargetSet TargetSet { get; init; }

    public required IReadOnlyList<string> RequiredPrefixes { get; init; }

    public bool CaseSensitive { get; init; }

    /// <summary>
    /// When set, limits which naming objects are validated for the required prefix.
    /// None validates both family and type names.
    /// </summary>
    public PrefixFixScope PrefixFixScope { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
