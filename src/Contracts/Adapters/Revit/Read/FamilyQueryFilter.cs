using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Category-based filter criteria for family retrieval.
/// </summary>
public sealed record FamilyCategoryFilter
{
    public IReadOnlyList<string>? CategoryNames { get; init; }

    public IReadOnlyList<string>? CategoryIdentifiers { get; init; }
}

/// <summary>
/// Name-based filter criteria for family retrieval.
/// </summary>
public sealed record FamilyNameFilter
{
    public IReadOnlyList<string>? ExactNames { get; init; }

    public string? NamePattern { get; init; }
}

/// <summary>
/// Parameter-based filter criteria for family retrieval.
/// </summary>
public sealed record FamilyParameterFilter
{
    public required string ParameterName { get; init; }

    public string? ExpectedValue { get; init; }

    public bool? MustExist { get; init; }
}

/// <summary>
/// Relationship-based filter criteria for family retrieval.
/// </summary>
public sealed record FamilyRelationshipFilter
{
    public NormalizedRelationshipType? RelationshipType { get; init; }

    public string? TargetKind { get; init; }

    public string? TargetIdentifier { get; init; }
}

/// <summary>
/// Usage-based filter criteria for family retrieval.
/// </summary>
public sealed record FamilyUsageFilter
{
    public bool? IncludeUnused { get; init; }

    public bool? IncludeInPlace { get; init; }

    public bool? IncludeNested { get; init; }
}

/// <summary>
/// Filter criteria applied to a family retrieval query.
/// </summary>
public sealed record FamilyQueryFilter
{
    public FamilyCategoryFilter? Category { get; init; }

    public FamilyNameFilter? Name { get; init; }

    public FamilyParameterFilter? Parameter { get; init; }

    public FamilyRelationshipFilter? Relationship { get; init; }

    public FamilyUsageFilter? Usage { get; init; }
}
