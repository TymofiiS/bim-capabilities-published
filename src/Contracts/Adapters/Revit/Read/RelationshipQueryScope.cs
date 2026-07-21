namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Scope classification for a relationship retrieval query.
/// </summary>
public enum RelationshipQueryScopeKind
{
    EntireModel,
    SelectedElements,
    SelectedFamilies,
    SelectedFamilyTypes,
    Custom
}

/// <summary>
/// Execution scope for a relationship retrieval query.
/// </summary>
public sealed record RelationshipQueryScope
{
    public required RelationshipQueryScopeKind Kind { get; init; }

    public IReadOnlyList<string>? ScopeIdentifiers { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
