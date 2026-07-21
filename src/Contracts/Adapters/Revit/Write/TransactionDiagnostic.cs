namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Diagnostic emitted during transaction preparation or execution.
/// </summary>
public sealed record TransactionDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public TransactionDiagnosticSeverity Severity { get; init; }

    public string? TransactionId { get; init; }

    public string? Location { get; init; }

    public IReadOnlyDictionary<string, string>? Data { get; init; }
}
