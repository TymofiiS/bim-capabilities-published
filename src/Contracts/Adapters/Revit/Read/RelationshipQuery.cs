namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Input for retrieving normalized relationships from the Revit Adapter read layer.
/// </summary>
public sealed record RelationshipQuery
{
    public IReadOnlyList<string>? SourceObjects { get; init; }

    public IReadOnlyList<string>? TargetObjects { get; init; }

    public IReadOnlyList<RelationshipType>? RelationshipTypes { get; init; }

    public RelationshipQueryScope? Scope { get; init; }

    public RelationshipQueryFilter? Filter { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public string? CorrelationId { get; init; }
}
