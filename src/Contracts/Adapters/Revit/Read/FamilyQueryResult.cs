using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Aggregate statistics for a family retrieval query.
/// </summary>
public sealed record FamilyQueryStatistics
{
    public int TotalFamilies { get; init; }

    public int RetrievedFamilies { get; init; }

    public int FilteredFamilies { get; init; }

    public IReadOnlyDictionary<string, int>? CountsByCategory { get; init; }
}

/// <summary>
/// Metadata describing a family retrieval query execution.
/// </summary>
public sealed record FamilyQueryMetadata
{
    public string? CorrelationId { get; init; }

    public DateTimeOffset ExecutedAt { get; init; }

    public string? ProviderId { get; init; }

    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}

/// <summary>
/// Result of a family retrieval query.
/// </summary>
public sealed record FamilyQueryResult
{
    public required IReadOnlyList<NormalizedFamily> Families { get; init; }

    public IReadOnlyList<NormalizedPlacedInstance> PlacedInstances { get; init; } = [];

    public IReadOnlyList<FamilyQueryDiagnostic>? Diagnostics { get; init; }

    public FamilyQueryStatistics? Statistics { get; init; }

    public FamilyQueryMetadata? QueryMetadata { get; init; }
}
