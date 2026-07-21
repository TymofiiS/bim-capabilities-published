using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Contracts.Engines.Family.ImportedCad;

/// <summary>
/// Configuration for imported CAD detection severity.
/// </summary>
public sealed record ImportedCadDetectionConfiguration
{
    public EvidenceSeverity FailureSeverity { get; init; } = EvidenceSeverity.Error;
}

/// <summary>
/// Input for the Family Engine imported CAD detection atom.
/// </summary>
public sealed record ImportedCadDetectionRequest
{
    public required IReadOnlyList<NormalizedFamily> Families { get; init; }

    public RelationshipQueryResult? RelationshipQueryResult { get; init; }

    public ImportedCadDetectionConfiguration? Configuration { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
