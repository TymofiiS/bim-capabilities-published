using BIMCapabilities.Contracts.Engines.Family;
using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;

namespace BIMCapabilities.Contracts.Engines.Family.Filtering;

/// <summary>
/// Input for Family Engine filtering atoms.
/// </summary>
public sealed record FamilyFilterRequest
{
    public required SelectionContracts.FamilySelectionResult SelectionResult { get; init; }

    public FamilySelectionCriteria? Criteria { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
