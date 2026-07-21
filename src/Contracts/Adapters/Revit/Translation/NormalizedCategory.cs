namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

/// <summary>
/// Platform-neutral category metadata for normalized adapter objects.
/// </summary>
public sealed record NormalizedCategory
{
    public required NormalizedIdentifier Identifier { get; init; }

    public required string Name { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
