namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

/// <summary>
/// Platform-neutral representation of a placed family instance in the active model.
/// </summary>
public sealed record NormalizedPlacedInstance
{
    public required NormalizedIdentifier Identity { get; init; }

    public string? Name { get; init; }

    public required string FamilyName { get; init; }

    public required string FamilyTypeName { get; init; }

    public string? CategoryName { get; init; }

    public IReadOnlyList<NormalizedParameter>? Parameters { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
