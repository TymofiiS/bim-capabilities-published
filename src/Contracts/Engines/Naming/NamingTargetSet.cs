using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Engines.Naming;

/// <summary>
/// Normalized target set consumed by Naming Engine validation operations.
/// </summary>
public sealed record NamingTargetSet
{
    public required string TargetSetId { get; init; }

    public IReadOnlyList<NormalizedFamily>? TargetFamilies { get; init; }

    public IReadOnlyList<NormalizedFamilyType>? TargetTypes { get; init; }

    public IReadOnlyList<NormalizedCategory>? Categories { get; init; }

    public IReadOnlyDictionary<string, string>? SelectionMetadata { get; init; }
}
