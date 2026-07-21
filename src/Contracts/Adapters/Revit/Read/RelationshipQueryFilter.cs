namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Relationship type filter criteria for relationship retrieval.
/// </summary>
public sealed record RelationshipTypeFilter
{
    public IReadOnlyList<RelationshipType>? RelationshipTypes { get; init; }

    public bool? IncludeCustom { get; init; }
}

/// <summary>
/// Source-side filter criteria for relationship retrieval.
/// </summary>
public sealed record RelationshipSourceFilter
{
    public IReadOnlyList<string>? SourceIdentifiers { get; init; }

    public string? SourceKind { get; init; }
}

/// <summary>
/// Target-side filter criteria for relationship retrieval.
/// </summary>
public sealed record RelationshipTargetFilter
{
    public IReadOnlyList<string>? TargetIdentifiers { get; init; }

    public string? TargetKind { get; init; }
}

/// <summary>
/// Category-based filter criteria for relationship retrieval.
/// </summary>
public sealed record RelationshipCategoryFilter
{
    public IReadOnlyList<string>? CategoryNames { get; init; }

    public IReadOnlyList<string>? CategoryIdentifiers { get; init; }
}

/// <summary>
/// Depth-based filter criteria for relationship retrieval.
/// </summary>
public sealed record RelationshipDepthFilter
{
    public int? MaxDepth { get; init; }

    public int? MinDepth { get; init; }
}

/// <summary>
/// Filter criteria applied to a relationship retrieval query.
/// </summary>
public sealed record RelationshipQueryFilter
{
    public RelationshipTypeFilter? RelationshipType { get; init; }

    public RelationshipSourceFilter? Source { get; init; }

    public RelationshipTargetFilter? Target { get; init; }

    public RelationshipCategoryFilter? Category { get; init; }

    public RelationshipDepthFilter? Depth { get; init; }
}
