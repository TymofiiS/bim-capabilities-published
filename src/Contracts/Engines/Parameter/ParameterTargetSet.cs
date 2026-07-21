using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Engines.Parameter;

/// <summary>
/// Normalized target set consumed by Parameter Engine validation operations.
/// </summary>
public sealed record ParameterTargetSet
{
    public required string TargetSetId { get; init; }

    public IReadOnlyList<NormalizedFamily>? TargetFamilies { get; init; }

    public IReadOnlyList<NormalizedFamilyType>? TargetTypes { get; init; }

    public IReadOnlyList<NormalizedPlacedInstance>? TargetInstances { get; init; }

    public IReadOnlyList<NormalizedParameter>? TargetParameters { get; init; }

    public IReadOnlyDictionary<string, string>? SelectionMetadata { get; init; }
}
