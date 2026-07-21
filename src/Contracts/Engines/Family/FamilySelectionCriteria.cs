using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Engines.Family;

/// <summary>
/// Category-based selection criteria for Family Engine operations.
/// </summary>
public sealed record FamilyCategoryCriteria
{
    public IReadOnlyList<string>? CategoryNames { get; init; }

    public IReadOnlyList<string>? CategoryIdentifiers { get; init; }
}

/// <summary>
/// Name-based selection criteria for Family Engine operations.
/// </summary>
public sealed record FamilyNameCriteria
{
    public IReadOnlyList<string>? ExactNames { get; init; }

    public string? NamePattern { get; init; }
}

/// <summary>
/// Parameter-based selection criteria for Family Engine operations.
/// </summary>
public sealed record FamilyParameterCriteria
{
    public IReadOnlyList<string>? ParameterNames { get; init; }

    public bool? MustExist { get; init; }
}

/// <summary>
/// Relationship-based selection criteria for Family Engine operations.
/// </summary>
public sealed record FamilyRelationshipCriteria
{
    public IReadOnlyList<NormalizedRelationshipType>? RelationshipTypes { get; init; }

    public string? TargetKind { get; init; }
}

/// <summary>
/// Usage-based selection criteria for Family Engine operations.
/// </summary>
public sealed record FamilyUsageCriteria
{
    public bool? IncludeUnused { get; init; }

    public bool? IncludeInPlace { get; init; }

    public bool? IncludeNested { get; init; }
}

/// <summary>
/// Custom selection criteria for Family Engine operations.
/// </summary>
public sealed record FamilyCustomCriteria
{
    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}

/// <summary>
/// Criteria applied when selecting families for Family Engine operations.
/// </summary>
public sealed record FamilySelectionCriteria
{
    public FamilyCategoryCriteria? Categories { get; init; }

    public FamilyNameCriteria? Names { get; init; }

    public FamilyParameterCriteria? Parameters { get; init; }

    public FamilyRelationshipCriteria? Relationships { get; init; }

    public FamilyUsageCriteria? Usage { get; init; }

    public FamilyCustomCriteria? Custom { get; init; }
}
