namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Result of write request or batch execution reported by the adapter.
/// </summary>
public sealed record WriteRequestResult
{
    public required WriteRequestStatus Status { get; init; }

    public IReadOnlyList<WriteRequestDiagnostic>? Diagnostics { get; init; }

    public IReadOnlyList<WriteRequestReference>? RequestReferences { get; init; }

    public WriteRequestExecutionMetadata? ExecutionMetadata { get; init; }
}
