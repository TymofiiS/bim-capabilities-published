namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Ordered collection of transactions submitted for adapter execution.
/// </summary>
public sealed record TransactionBatch
{
    public required string BatchId { get; init; }

    public required IReadOnlyList<TransactionRequest> Transactions { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
