namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Canonical transaction request contract grouping write requests for adapter execution.
/// </summary>
public interface ITransactionRequest
{
    string TransactionId { get; }

    string Name { get; }

    string? Description { get; }

    IReadOnlyList<WriteRequest> WriteRequests { get; }

    TransactionScope? Scope { get; }

    IReadOnlyDictionary<string, string>? Metadata { get; }

    string? CorrelationId { get; }

    string? RuleId { get; }

    int Order { get; }

    DateTimeOffset RequestedAt { get; }
}
