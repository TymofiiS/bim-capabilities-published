namespace BIMCapabilities.Contracts.Engines.Parameter;

/// <summary>
/// Input for Parameter Engine validation operations.
/// </summary>
public sealed record ParameterValidationRequest
{
    public required ParameterTargetSet TargetSet { get; init; }

    public required ParameterValidationCriteria Criteria { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
