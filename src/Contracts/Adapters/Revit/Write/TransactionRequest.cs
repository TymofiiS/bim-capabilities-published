namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Deterministic transaction grouping write requests for adapter execution.
/// </summary>
public sealed record TransactionRequest : ITransactionRequest
{
    public required string TransactionId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required IReadOnlyList<WriteRequest> WriteRequests { get; init; }

    public TransactionScope? Scope { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public string? CorrelationId { get; init; }

    public string? RuleId { get; init; }

    public int Order { get; init; }

    public DateTimeOffset RequestedAt { get; init; }
}
