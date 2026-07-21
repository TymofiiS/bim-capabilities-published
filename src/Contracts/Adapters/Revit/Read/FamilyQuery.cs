namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Input for retrieving normalized families from the Revit Adapter read layer.
/// </summary>
public sealed record FamilyQuery
{
    public IReadOnlyList<string>? Categories { get; init; }

    public IReadOnlyList<string>? FamilyNames { get; init; }

    public IReadOnlyList<string>? FamilyTypeNames { get; init; }

    public FamilyQueryScope? Scope { get; init; }

    public FamilyQueryFilter? Filter { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public string? CorrelationId { get; init; }
}
