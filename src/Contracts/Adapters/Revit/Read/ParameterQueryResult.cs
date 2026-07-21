using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Aggregate statistics for a parameter retrieval query.
/// </summary>
public sealed record ParameterQueryStatistics
{
    public int TotalParameters { get; init; }

    public int RetrievedParameters { get; init; }

    public int FilteredParameters { get; init; }

    public int MissingParameters { get; init; }

    public IReadOnlyDictionary<string, int>? CountsByParameterName { get; init; }
}

/// <summary>
/// Metadata describing a parameter retrieval query execution.
/// </summary>
public sealed record ParameterQueryMetadata
{
    public string? CorrelationId { get; init; }

    public DateTimeOffset ExecutedAt { get; init; }

    public string? ProviderId { get; init; }

    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}

/// <summary>
/// Result of a parameter retrieval query.
/// </summary>
public sealed record ParameterQueryResult
{
    public required IReadOnlyList<NormalizedParameter> Parameters { get; init; }

    public IReadOnlyList<ParameterQueryDiagnostic>? Diagnostics { get; init; }

    public ParameterQueryStatistics? Statistics { get; init; }

    public ParameterQueryMetadata? QueryMetadata { get; init; }
}
