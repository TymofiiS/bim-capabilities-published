using BIMCapabilities.Contracts.Engines.Family.Discovery;

namespace BIMCapabilities.Contracts.Engines.Family.Selection;

/// <summary>
/// Input for Family Engine selection atoms.
/// </summary>
public sealed record FamilySelectionRequest
{
    public required FamilyDiscoveryResult DiscoveryResult { get; init; }

    public FamilySelectionCriteria? Criteria { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
