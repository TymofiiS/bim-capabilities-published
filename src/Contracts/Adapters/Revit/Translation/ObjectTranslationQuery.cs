namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

/// <summary>
/// Input for translating a source object into normalized adapter contracts.
/// </summary>
public sealed record ObjectTranslationQuery
{
    public required string SourceObjectId { get; init; }

    public required string SourceKind { get; init; }

    public string? CorrelationId { get; init; }
}

/// <summary>
/// Result of translating a source object into normalized adapter contracts.
/// </summary>
public sealed record ObjectTranslationResult
{
    public NormalizedObject? Object { get; init; }

    public NormalizedFamily? Family { get; init; }

    public NormalizedFamilyType? FamilyType { get; init; }

    public NormalizedCategory? Category { get; init; }

    public NormalizedParameter? Parameter { get; init; }

    public NormalizedRelationship? Relationship { get; init; }
}
