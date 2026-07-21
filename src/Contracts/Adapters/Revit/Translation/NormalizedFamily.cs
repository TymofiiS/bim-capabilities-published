namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

/// <summary>
/// Platform-neutral representation of a family translated by the Revit Adapter.
/// </summary>
public sealed record NormalizedFamily
{
    public required NormalizedIdentifier Identity { get; init; }

    public required string Name { get; init; }

    public NormalizedCategory? Category { get; init; }

    public IReadOnlyList<NormalizedFamilyType>? FamilyTypes { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public IReadOnlyList<NormalizedRelationship>? Relationships { get; init; }

    public IReadOnlyList<NormalizedParameter>? Parameters { get; init; }
}
