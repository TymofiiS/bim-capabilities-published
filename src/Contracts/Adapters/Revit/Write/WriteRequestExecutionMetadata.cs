namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Metadata describing write request execution.
/// </summary>
public sealed record WriteRequestExecutionMetadata
{
    public DateTimeOffset? ExecutedAt { get; init; }

    public string? CorrelationId { get; init; }

    public string? BatchId { get; init; }

    public string? AdapterId { get; init; }

    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}
