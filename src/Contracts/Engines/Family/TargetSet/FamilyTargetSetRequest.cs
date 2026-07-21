using BIMCapabilities.Contracts.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Engines.Family;
using BIMCapabilities.Contracts.Engines.Family.ImportedCad;

namespace BIMCapabilities.Contracts.Engines.Family.TargetSet;

/// <summary>
/// Imported CAD compliance mode applied when generating a target set.
/// </summary>
public enum ImportedCadComplianceMode
{
    None,

    RequireImportedCad,

    ExcludeImportedCad
}

/// <summary>
/// Compliance criteria applied when generating a target set.
/// </summary>
public sealed record TargetSetComplianceCriteria
{
    public ImportedCadComplianceMode ImportedCadMode { get; init; } = ImportedCadComplianceMode.None;
}

/// <summary>
/// Definition of a Family Engine target set generation operation.
/// </summary>
public sealed record TargetSetDefinition
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public FamilySelectionCriteria? SelectionCriteria { get; init; }

    public FamilySelectionCriteria? FilteringCriteria { get; init; }

    public TargetSetComplianceCriteria? ComplianceCriteria { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Input for Family Engine target set generation.
/// </summary>
public sealed record FamilyTargetSetRequest
{
    public required TargetSetDefinition Definition { get; init; }

    public RelationshipQueryResult? RelationshipQueryResult { get; init; }

    public ImportedCadDetectionConfiguration? ImportedCadConfiguration { get; init; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public string? RuleId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
