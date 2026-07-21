namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

/// <summary>
/// Platform-neutral relationship between normalized adapter objects.
/// </summary>
public sealed record NormalizedRelationship
{
    public required NormalizedIdentifier Source { get; init; }

    public required NormalizedIdentifier Target { get; init; }

    public required NormalizedRelationshipType RelationshipType { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
