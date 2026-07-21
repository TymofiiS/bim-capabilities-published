using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Parameter.Value;

/// <summary>
/// Validation rule applied to a single parameter value.
/// </summary>
public sealed record ParameterValueRule
{
    public required string ParameterName { get; init; }

    public bool RequiredValue { get; init; }

    public IReadOnlyList<string>? AllowedValues { get; init; }

    public IReadOnlyList<string>? ForbiddenValues { get; init; }

    public int? MinimumLength { get; init; }

    public int? MaximumLength { get; init; }

    public string? RegularExpression { get; init; }

    public string? CustomRuleIdentifier { get; init; }

    public EvidenceSeverity Severity { get; init; } = EvidenceSeverity.Error;
}
