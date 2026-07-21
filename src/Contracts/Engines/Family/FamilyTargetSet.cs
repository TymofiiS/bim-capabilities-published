using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Engines.Family;

/// <summary>
/// Normalized target set used by Family Engine discovery, selection, and filtering.
/// </summary>
public sealed record FamilyTargetSet
{
    public required string TargetSetId { get; init; }

    public IReadOnlyList<NormalizedFamily>? Families { get; init; }

    public IReadOnlyList<NormalizedFamilyType>? FamilyTypes { get; init; }

    public IReadOnlyList<NormalizedPlacedInstance>? PlacedInstances { get; init; }

    public IReadOnlyList<NormalizedCategory>? Categories { get; init; }

    public IReadOnlyList<NormalizedRelationship>? Relationships { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
