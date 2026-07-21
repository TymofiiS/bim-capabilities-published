namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Ordered collection of write requests submitted for adapter execution.
/// </summary>
public sealed record WriteRequestBatch
{
    public required string BatchId { get; init; }

    public required IReadOnlyList<WriteRequest> Requests { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
