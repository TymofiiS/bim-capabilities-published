namespace BIMCapabilities.Contracts.Engines.Naming;

/// <summary>
/// Input for Naming Engine validation operations.
/// </summary>
public sealed record NamingValidationRequest
{
    public required NamingTargetSet TargetSet { get; init; }

    public required NamingValidationCriteria Criteria { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
