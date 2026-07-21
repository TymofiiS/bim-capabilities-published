namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Execution scope for a family retrieval query.
/// </summary>
public sealed record FamilyQueryScope
{
    public required FamilyQueryScopeKind Kind { get; init; }

    public IReadOnlyList<string>? ScopeIdentifiers { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
