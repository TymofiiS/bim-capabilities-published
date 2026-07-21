namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

/// <summary>
/// Platform-neutral representation of a family type translated by the Revit Adapter.
/// </summary>
public sealed record NormalizedFamilyType
{
    public required NormalizedIdentifier Identity { get; init; }

    public required string Name { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public IReadOnlyList<NormalizedParameter>? Parameters { get; init; }
}
