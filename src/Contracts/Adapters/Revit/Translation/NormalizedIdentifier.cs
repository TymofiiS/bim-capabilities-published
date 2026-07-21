namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

/// <summary>
/// Platform-neutral identity for a normalized adapter object.
/// </summary>
public sealed record NormalizedIdentifier
{
    public required string Id { get; init; }

    public string? Kind { get; init; }

    public string? Scope { get; init; }
}
