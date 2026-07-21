using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Aggregate statistics for a relationship retrieval query.
/// </summary>
public sealed record RelationshipQueryStatistics
{
    public int TotalRelationships { get; init; }

    public int RetrievedRelationships { get; init; }

    public int FilteredRelationships { get; init; }

    public IReadOnlyDictionary<string, int>? CountsByRelationshipType { get; init; }
}

/// <summary>
/// Metadata describing a relationship retrieval query execution.
/// </summary>
public sealed record RelationshipQueryMetadata
{
    public string? CorrelationId { get; init; }

    public DateTimeOffset ExecutedAt { get; init; }

    public string? ProviderId { get; init; }

    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}

/// <summary>
/// Result of a relationship retrieval query.
/// </summary>
public sealed record RelationshipQueryResult
{
    public required IReadOnlyList<NormalizedRelationship> Relationships { get; init; }

    public IReadOnlyList<RelationshipQueryDiagnostic>? Diagnostics { get; init; }

    public RelationshipQueryStatistics? Statistics { get; init; }

    public RelationshipQueryMetadata? QueryMetadata { get; init; }
}
