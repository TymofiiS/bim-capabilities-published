namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Result of transaction execution reported by the adapter.
/// </summary>
public sealed record TransactionResult
{
    public required TransactionStatus Status { get; init; }

    public IReadOnlyList<WriteRequestReference>? ExecutedRequests { get; init; }

    public IReadOnlyList<TransactionDiagnostic>? Diagnostics { get; init; }

    public TransactionExecutionMetadata? ExecutionMetadata { get; init; }
}
