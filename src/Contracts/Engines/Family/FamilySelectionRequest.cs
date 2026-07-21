namespace BIMCapabilities.Contracts.Engines.Family;

/// <summary>
/// Input for Family Engine selection operations.
/// </summary>
public sealed record FamilySelectionRequest
{
    public required FamilySelectionCriteria Criteria { get; init; }

    public FamilyTargetSet? SourceTargetSet { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
